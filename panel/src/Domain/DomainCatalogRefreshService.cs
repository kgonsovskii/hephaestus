using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Domain;

public sealed class DomainCatalogRefreshService : BackgroundService
{
    private readonly IDomainRepository _repository;
    private readonly DomainCatalog _catalog;
    private readonly ILogger<DomainCatalogRefreshService> _logger;
    private readonly TimeSpan _interval;
    private readonly IDomainHostsChangedSignal _hostsChanged;

    public DomainCatalogRefreshService(
        IDomainRepository repository,
        DomainCatalog catalog,
        IOptions<DomainHostOptions> options,
        ILogger<DomainCatalogRefreshService> logger,
        IDomainHostsChangedSignal hostsChanged)
    {
        _repository = repository;
        _catalog = catalog;
        _logger = logger;
        _hostsChanged = hostsChanged;
        var sec = Math.Clamp(options.Value.RefreshSeconds, 5, 3600);
        _interval = TimeSpan.FromSeconds(sec);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshOnce(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var delayTask = Task.Delay(_interval, linked.Token);
                var wakeTask = _hostsChanged.WhenCatalogWakeAsync(stoppingToken);
                var winner = await Task.WhenAny(delayTask, wakeTask).ConfigureAwait(false);
                if (winner == wakeTask)
                {
                    linked.Cancel();
                    _hostsChanged.DrainExtraCatalogSignals();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RefreshOnce(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshOnce(CancellationToken ct)
    {
        try
        {
            var rows = await _repository.LoadEnabledDomainsAsync(ct).ConfigureAwait(false);
            _catalog.Replace(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh domain catalog from domains.json.");
        }
    }
}
