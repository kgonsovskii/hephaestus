using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Cloner;

public sealed class ClonerRemoteInstallService : IClonerRemoteInstall
{
    /// <summary>At most one job queued; worker runs one at a time. New start preempts prior run and any queued job.</summary>
    private readonly Channel<RemoteInstallJob> _queue = Channel.CreateBounded<RemoteInstallJob>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait });

    private readonly ConcurrentDictionary<Guid, Channel<string>> _logReaders = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellations = new();
    private readonly ILogger<ClonerRemoteInstallService> _logger;
    private readonly object _startLock = new();

    public ClonerRemoteInstallService(ILogger<ClonerRemoteInstallService> logger) => _logger = logger;

    internal ChannelReader<RemoteInstallJob> JobReader => _queue.Reader;

    public async Task<Guid> StartRemoteInstallAsync(string host, string user, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("User is required.", nameof(user));

        lock (_startLock)
        {
            var prior = _runCancellations.Keys.ToArray();
            foreach (var runId in prior)
                TryStop(runId);
            if (prior.Length > 0)
                _logger.LogInformation("Cloner: stopped {Count} prior install(s) for new start", prior.Length);
        }

        var runIdNew = Guid.NewGuid();
        var logCh = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
        _logReaders[runIdNew] = logCh;

        var runCts = new CancellationTokenSource();
        _runCancellations[runIdNew] = runCts;

        var job = new RemoteInstallJob(runIdNew, host.Trim(), user.Trim(), password ?? "", logCh.Writer, runCts.Token);
        await _queue.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cloner: queued remote install {RunId} for {Host}", runIdNew, host);
        return runIdNew;
    }

    public ChannelReader<string>? TrySubscribeLogReader(Guid runId) =>
        _logReaders.TryGetValue(runId, out var ch) ? ch.Reader : null;

    public bool TryStop(Guid runId)
    {
        if (!_runCancellations.ContainsKey(runId))
            return false;

        if (_runCancellations.TryGetValue(runId, out var cts))
            cts.Cancel();

        return true;
    }

    internal void CompleteRun(Guid runId)
    {
        _logReaders.TryRemove(runId, out _);
        if (_runCancellations.TryRemove(runId, out var cts))
        {
            try
            {
                cts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cloner: dispose run cts for {RunId}", runId);
            }
        }
    }
}
