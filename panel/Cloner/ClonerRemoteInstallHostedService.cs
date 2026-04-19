using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cloner;

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
        await foreach (var job in _coordinator.JobReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            await RunJobAsync(job, stoppingToken).ConfigureAwait(false);
    }

    private async Task RunJobAsync(RemoteInstallJob job, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.RunCancellationToken);
        var ct = linked.Token;

        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Cloner: install {RunId} -> {Host}", job.RunId, job.Host);

            var exit = await _executor.ExecuteAsync(job, ct).ConfigureAwait(false);
            await WriteLogLineAsync(job, $"[exit] {exit}", ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!stoppingToken.IsCancellationRequested)
                await WriteLogLineAsync(job, "[stopped]", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloner: install failed {RunId}", job.RunId);
            await WriteLogLineAsync(job, $"[error] {ex.Message}", stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            job.LogWriter.TryComplete();
            _coordinator.CompleteRun(job.RunId);
        }
    }

    private static async Task WriteLogLineAsync(RemoteInstallJob job, string line, CancellationToken cancellationToken)
    {
        try
        {
            await job.LogWriter.WriteAsync(line, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }
    }
}
