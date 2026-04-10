using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Refiner;

/// <summary>
/// Runs <see cref="IStatsMaintenance"/> and <see cref="IDomainMaintenance"/> on independent timers
/// (parallel loops, each using <see cref="RefinerOptions.StatsInterval"/> or <see cref="RefinerOptions.DomainInterval"/>).
/// </summary>
public sealed class RefinerBackgroundService : BackgroundService
{
    private readonly ILogger<RefinerBackgroundService> _logger;
    private readonly IOptionsMonitor<RefinerOptions> _options;
    private readonly IStatsMaintenance _statsMaintenance;
    private readonly IDomainMaintenance _domainMaintenance;

    public RefinerBackgroundService(
        ILogger<RefinerBackgroundService> logger,
        IOptionsMonitor<RefinerOptions> options,
        IStatsMaintenance statsMaintenance,
        IDomainMaintenance domainMaintenance)
    {
        _logger = logger;
        _options = options;
        _statsMaintenance = statsMaintenance;
        _domainMaintenance = domainMaintenance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
                RunLoopAsync(_statsMaintenance, () => _options.CurrentValue.StatsInterval, "stats", stoppingToken),
                RunLoopAsync(_domainMaintenance, () => _options.CurrentValue.DomainInterval, "domain", stoppingToken))
            .ConfigureAwait(false);
    }

    private async Task RunLoopAsync(
        IMaintenance maintenance,
        Func<TimeSpan> getInterval,
        string label,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = getInterval();
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromMinutes(1);

            try
            {
                await maintenance.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refiner {Label} maintenance failed", label);
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
