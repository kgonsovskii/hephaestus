using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Cloner;

public sealed class ClonerRemoteInstallService : IClonerRemoteInstall
{
    private readonly Channel<RemoteInstallJob> _queue = Channel.CreateBounded<RemoteInstallJob>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait });

    private readonly ConcurrentDictionary<Guid, Channel<string>> _logReaders = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellations = new();
    private readonly ConcurrentDictionary<Guid, Process> _activeProcesses = new();
    private readonly ILogger<ClonerRemoteInstallService> _logger;

    public ClonerRemoteInstallService(ILogger<ClonerRemoteInstallService> logger) => _logger = logger;

    internal ChannelReader<RemoteInstallJob> JobReader => _queue.Reader;

    public async Task<Guid> StartRemoteInstallAsync(string host, string user, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("User is required.", nameof(user));

        var runId = Guid.NewGuid();
        var logCh = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
        _logReaders[runId] = logCh;

        var runCts = new CancellationTokenSource();
        _runCancellations[runId] = runCts;

        var job = new RemoteInstallJob(runId, host.Trim(), user.Trim(), password ?? "", logCh.Writer, runCts.Token);
        await _queue.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cloner: queued remote install {RunId} for {Host}", runId, host);
        return runId;
    }

    public ChannelReader<string>? TrySubscribeLogReader(Guid runId) =>
        _logReaders.TryGetValue(runId, out var ch) ? ch.Reader : null;

    public bool TryStop(Guid runId)
    {
        var known = _runCancellations.ContainsKey(runId);
        if (!known)
            return false;

        if (_activeProcesses.TryGetValue(runId, out var proc))
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cloner: kill process for {RunId}", runId);
            }
        }

        if (_runCancellations.TryGetValue(runId, out var cts))
            cts.Cancel();

        return true;
    }

    internal void AttachRunningProcess(Guid runId, Process process) => _activeProcesses[runId] = process;

    internal void DetachRunningProcess(Guid runId) => _activeProcesses.TryRemove(runId, out _);

    internal void CompleteRun(Guid runId)
    {
        _logReaders.TryRemove(runId, out _);
        DetachRunningProcess(runId);
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
