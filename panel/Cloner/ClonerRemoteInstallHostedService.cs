using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cloner;

/// <summary>Runs remote install work off the HTTP thread so the site stays responsive.</summary>
public sealed class ClonerRemoteInstallHostedService : BackgroundService
{
    private readonly ClonerRemoteInstallService _coordinator;
    private readonly IClonerInstallExecutor _executor;
    private readonly ILogger<ClonerRemoteInstallHostedService> _logger;

    public ClonerRemoteInstallHostedService(
        ClonerRemoteInstallService coordinator,
        IClonerInstallExecutor executor,
        ILogger<ClonerRemoteInstallHostedService> logger)
    {
        _coordinator = coordinator;
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in _coordinator.InstallHandoffReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            await RunInstallAsync(work, stoppingToken).ConfigureAwait(false);
    }

    private async Task RunInstallAsync(RemoteInstallWork work, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, work.RunCancellationToken);
        var ct = linked.Token;

        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Cloner: install {RunId} -> {Host}", work.RunId, work.Host);

            var exit = await _executor.ExecuteAsync(work, ct).ConfigureAwait(false);
            await WriteLogLineAsync(work, $"[exit] {exit}", ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!stoppingToken.IsCancellationRequested)
                await WriteLogLineAsync(work, "[stopped]", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloner: install failed {RunId}", work.RunId);
            await WriteLogLineAsync(work, $"[error] {ex.Message}", stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            work.LogWriter.TryComplete();
            _coordinator.CompleteRun(work.RunId);
        }
    }

    private static async Task WriteLogLineAsync(RemoteInstallWork work, string line, CancellationToken cancellationToken)
    {
        try
        {
            await work.LogWriter.WriteAsync(line, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }
    }
}
