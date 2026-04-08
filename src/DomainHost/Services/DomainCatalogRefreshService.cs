using DomainHost.Configuration;
using DomainHost.Data;
using Microsoft.Extensions.Options;

namespace DomainHost.Services;

public sealed class DomainCatalogRefreshService : BackgroundService
{
    private readonly IDomainRepository _repository;
    private readonly DomainCatalog _catalog;
    private readonly ILogger<DomainCatalogRefreshService> _logger;
    private readonly TimeSpan _interval;

    public DomainCatalogRefreshService(
        IDomainRepository repository,
        DomainCatalog catalog,
        IOptions<DomainHostOptions> options,
        ILogger<DomainCatalogRefreshService> logger)
    {
        _repository = repository;
        _catalog = catalog;
        _logger = logger;
        var sec = Math.Clamp(options.Value.RefreshSeconds, 5, 3600);
        _interval = TimeSpan.FromSeconds(sec);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshOnce(stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                await RefreshOnce(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshOnce(CancellationToken ct)
    {
        try
        {
            var rows = await _repository.LoadEnabledDomainsAsync(ct).ConfigureAwait(false);
            _catalog.Replace(rows);
            _logger.LogInformation("Domain catalog refreshed: {Count} host(s).", rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh domain catalog from domains.json.");
        }
    }
}
