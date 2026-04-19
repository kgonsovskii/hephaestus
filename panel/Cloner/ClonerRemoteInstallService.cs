using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Cloner;

public sealed class ClonerRemoteInstallService : IClonerRemoteInstall
{
    private readonly Channel<RemoteInstallJob> _queue = Channel.CreateBounded<RemoteInstallJob>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait });

    private readonly ConcurrentDictionary<Guid, Channel<string>> _logReaders = new();
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

        var job = new RemoteInstallJob(runId, host.Trim(), user.Trim(), password ?? "", logCh.Writer);
        await _queue.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cloner: queued remote install {RunId} for {Host}", runId, host);
        return runId;
    }

    public ChannelReader<string>? TrySubscribeLogReader(Guid runId) =>
        _logReaders.TryGetValue(runId, out var ch) ? ch.Reader : null;

    internal void CompleteRun(Guid runId) => _logReaders.TryRemove(runId, out _);
}
