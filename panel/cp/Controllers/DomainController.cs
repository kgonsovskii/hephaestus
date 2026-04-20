using cp.Models;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

public class DomainController : BaseController
{
    public const string TempDataMessageKey = "DomainUiMessage";

    private readonly IDomainRepository _domains;
    private readonly DomainCatalog _catalog;
    private readonly IWebContentClassCatalog _classes;
    private readonly IDomainHostsChangedSignal _hostsChanged;

    public DomainController(
        ServerService serverService,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IDomainRepository domains,
        DomainCatalog catalog,
        IWebContentClassCatalog classes,
        IDomainHostsChangedSignal hostsChanged) : base(serverService, configuration, memoryCache)
    {
        _domains = domains;
        _catalog = catalog;
        _classes = classes;
        _hostsChanged = hostsChanged;
    }

    [HttpGet("domains")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await _domains.LoadAllDomainsAsync(cancellationToken).ConfigureAwait(false);
        var rows = list.Select(DomainEditRow.FromRecord).ToList();
        var names = _classes.ListClassFolderNames();
        var message = TempData[TempDataMessageKey] as string;
        return View(new DomainPageViewModel
        {
            DomainRows = rows,
            ClassFolderNames = names,
            Message = message
        });
    }

    [HttpPost("domains/save")]
    public async Task<IActionResult> Save(
        [FromForm(Name = "DomainRows")] List<DomainEditRow>? domainRows,
        CancellationToken cancellationToken)
    {
        var rows = domainRows ?? new List<DomainEditRow>();
        var records = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Domain))
            .Select(r => r.ToDomainRecord())
            .ToList();
        await _domains.SaveDomainsAsync(records, cancellationToken).ConfigureAwait(false);
        await ReplaceInMemoryCatalogAsync(cancellationToken).ConfigureAwait(false);
        RefineServerAndNotifyRefiner();
        TempData[TempDataMessageKey] = "Domains saved; hosted sync will run shortly.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("domains/bulk")]
    public async Task<IActionResult> Bulk(
        [FromForm] string? bulkDomainLines,
        [FromForm] string? bulkClass,
        CancellationToken cancellationToken)
    {
        var existing = await _domains.LoadAllDomainsAsync(cancellationToken).ConfigureAwait(false);
        var list = existing.Select(DomainEditRow.FromRecord).ToList();
        var seen = new HashSet<string>(list.Select(r => r.Domain.Trim()), StringComparer.OrdinalIgnoreCase);
        bulkClass = bulkClass?.Trim() ?? "";
        var lines = (bulkDomainLines ?? "").Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        var added = 0;
        foreach (var raw in lines)
        {
            var host = raw.Trim();
            if (host.Length == 0)
                continue;
            if (seen.Contains(host))
                continue;
            list.Add(new DomainEditRow
            {
                Enabled = true,
                Domain = host,
                Ip = null,
                DomainClass = bulkClass,
                ContentKind = "javascript",
                RedirectUrl = null
            });
            seen.Add(host);
            added++;
        }

        await _domains.SaveDomainsAsync(list.Select(r => r.ToDomainRecord()).ToList(), cancellationToken).ConfigureAwait(false);
        await ReplaceInMemoryCatalogAsync(cancellationToken).ConfigureAwait(false);
        RefineServerAndNotifyRefiner();

        TempData[TempDataMessageKey] = added == 0
            ? "No new domains added (duplicates or empty lines skipped); hosted sync will run shortly."
            : $"Added {added} domain(s); hosted sync will run shortly.";
        return RedirectToAction(nameof(Index));
    }

        private async Task ReplaceInMemoryCatalogAsync(CancellationToken cancellationToken)
    {
        var enabled = await _domains.LoadEnabledDomainsAsync(cancellationToken).ConfigureAwait(false);
        _catalog.Replace(enabled);
    }

    void RefineServerAndNotifyRefiner()
    {
        var server = _serverService.GetServerLite();
        _serverService.RefineCommonsAndSave(server);
        _hostsChanged.NotifyHostsChanged();
    }
}
