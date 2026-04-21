using Commons;
using Db;
using Domain;
using LandingFtp;
using TroyanMaintenance;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Refiner;

public sealed class RefinerBackgroundService : BackgroundService
{
    private readonly ILogger<RefinerBackgroundService> _logger;
    private readonly IOptionsMonitor<RefinerOptions> _options;
    private readonly IStatsMaintenance _statsMaintenance;
    private readonly IDomainMaintenance _domainMaintenance;
    private readonly ITroyanBuildMaintenance _troyanMaintenance;
    private readonly ILandingFtpMaintenance _landingFtpMaintenance;
    private readonly IDomainHostsChangedSignal _hostsChanged;

    public RefinerBackgroundService(
        ILogger<RefinerBackgroundService> logger,
        IOptionsMonitor<RefinerOptions> options,
        IStatsMaintenance statsMaintenance,
        IDomainMaintenance domainMaintenance,
        ITroyanBuildMaintenance troyanMaintenance,
        ILandingFtpMaintenance landingFtpMaintenance,
        IDomainHostsChangedSignal hostsChanged)
    {
        _logger = logger;
        _options = options;
        _statsMaintenance = statsMaintenance;
        _domainMaintenance = domainMaintenance;
        _troyanMaintenance = troyanMaintenance;
        _landingFtpMaintenance = landingFtpMaintenance;
        _hostsChanged = hostsChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
                RunLoopAsync(_statsMaintenance, () => _options.CurrentValue.StatsInterval, "stats", stoppingToken),
                RunDomainLoopWithWakeAsync(stoppingToken),
                RunTroyanLoopWithWakeAsync(stoppingToken),
                RunLandingFtpLoopWithWakeAsync(stoppingToken))
            .ConfigureAwait(false);
    }

        private async Task RunDomainLoopWithWakeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _domainMaintenance.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refiner {Label} maintenance failed", "domain");
            }

            var interval = _options.CurrentValue.DomainInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromMinutes(1);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var delayTask = Task.Delay(interval, linked.Token);
                var wakeTask = _hostsChanged.WhenRefinerWakeAsync(stoppingToken);
                var winner = await Task.WhenAny(delayTask, wakeTask).ConfigureAwait(false);
                if (winner == wakeTask)
                {
                    linked.Cancel();
                    _hostsChanged.DrainExtraRefinerSignals();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunTroyanLoopWithWakeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _troyanMaintenance.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refiner {Label} maintenance failed", "troyan");
            }

            var interval = _options.CurrentValue.TroyanInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromMinutes(1);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var delayTask = Task.Delay(interval, linked.Token);
                var wakeTask = _hostsChanged.WhenTroyanWakeAsync(stoppingToken);
                var winner = await Task.WhenAny(delayTask, wakeTask).ConfigureAwait(false);
                if (winner == wakeTask)
                {
                    linked.Cancel();
                    _hostsChanged.DrainExtraTroyanSignals();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunLandingFtpLoopWithWakeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _landingFtpMaintenance.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refiner {Label} maintenance failed", "landing ftp");
            }

            var interval = _options.CurrentValue.LandingFtpInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromMinutes(10);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var delayTask = Task.Delay(interval, linked.Token);
                var wakeTask = _hostsChanged.WhenLandingWakeAsync(stoppingToken);
                var winner = await Task.WhenAny(delayTask, wakeTask).ConfigureAwait(false);
                if (winner == wakeTask)
                {
                    linked.Cancel();
                    _hostsChanged.DrainExtraLandingSignals();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
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
