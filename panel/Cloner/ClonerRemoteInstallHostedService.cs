using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cloner;

public sealed class ClonerRemoteInstallHostedService : BackgroundService
{
    private readonly ClonerRemoteInstallService _coordinator;
    private readonly IClonerInstallExecutor _executor;
    private readonly IOptionsMonitor<ClonerOptions> _options;
    private readonly ILogger<ClonerRemoteInstallHostedService> _logger;

    public ClonerRemoteInstallHostedService(
        ClonerRemoteInstallService coordinator,
        IClonerInstallExecutor executor,
        IOptionsMonitor<ClonerOptions> options,
        ILogger<ClonerRemoteInstallHostedService> logger)
    {
        _coordinator = coordinator;
        _executor = executor;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _coordinator.JobReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunOneJobAsync(job, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await job.LogWriter.WriteAsync("[stopped]", CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (ChannelClosedException)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloner: install failed for {RunId}", job.RunId);
                try
                {
                    await job.LogWriter.WriteAsync($"[error] {ex.Message}", stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                job.LogWriter.TryComplete();
                _coordinator.CompleteRun(job.RunId);
            }
        }
    }

    private async Task RunOneJobAsync(RemoteInstallJob job, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.RunCancellationToken);
        var ct = linked.Token;
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Cloner: starting remote install {RunId} for {Host} (executor: {Executor})",
            job.RunId,
            job.Host,
            _options.CurrentValue.Executor);

        var exit = await _executor.ExecuteAsync(job, ct).ConfigureAwait(false);

        try
        {
            await job.LogWriter.WriteAsync($"[exit] {exit}", ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
