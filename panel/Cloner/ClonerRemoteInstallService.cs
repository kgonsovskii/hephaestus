using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using model;

namespace Cloner;

/// <summary>At most one remote install runs in the background; a new start cancels the previous run. No multi-job backlog.</summary>
public sealed class ClonerRemoteInstallService : IClonerRemoteInstall
{
    /// <summary>Single slot from HTTP to <see cref="ClonerRemoteInstallHostedService"/>: latest start wins if several happen before the worker picks up work.</summary>
    private readonly Channel<RemoteInstallWork> _installHandoff = Channel.CreateBounded<RemoteInstallWork>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

    private readonly ConcurrentDictionary<Guid, Channel<string>> _logReaders = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellations = new();
    private readonly ILogger<ClonerRemoteInstallService> _logger;
    private readonly object _startLock = new();

    public ClonerRemoteInstallService(ILogger<ClonerRemoteInstallService> logger) => _logger = logger;

    internal ChannelReader<RemoteInstallWork> InstallHandoffReader => _installHandoff.Reader;

    public async Task<Guid> StartRemoteInstallAsync(string host, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));
        if (CloneRemoteInstallTarget.ValidateHost(host) is { } hostErr)
            throw new ArgumentException(hostErr, nameof(host));

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

        var work = new RemoteInstallWork(runIdNew, host.Trim(), logCh.Writer, runCts.Token);
        await _installHandoff.Writer.WriteAsync(work, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cloner: remote install {RunId} for {Host} (background worker)", runIdNew, host);
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

    /// <summary>
    /// Log reader entries must outlive a fast-failing install: the browser opens the WebSocket after <c>POST /clone</c>
    /// returns, so removing the channel immediately races the UI and yields a failed upgrade with no log.
    /// </summary>
    private static readonly TimeSpan LogReaderRetentionAfterComplete = TimeSpan.FromSeconds(120);

    internal void CompleteRun(Guid runId)
    {
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

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LogReaderRetentionAfterComplete).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (_logReaders.TryRemove(runId, out _))
                _logger.LogDebug("Cloner: dropped log reader for completed {RunId}", runId);
        });
    }
}
