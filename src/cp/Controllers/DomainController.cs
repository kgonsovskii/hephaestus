using cp.Models;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using model;

namespace cp.Controllers;

[Authorize(Policy = "AllowFromIpRange")]
public class DomainController : BaseController
{
    public const string TempDataMessageKey = "DomainUiMessage";

    private readonly IDomainRepository _domains;
    private readonly IWebContentClassCatalog _classes;
    private readonly DomainCatalog _domainCatalog;
    private readonly IDomainMaintenance _domainMaintenance;
    private readonly ILogger<DomainController> _logger;

    public DomainController(
        ServerService serverService,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IDomainRepository domains,
        IWebContentClassCatalog classes,
        DomainCatalog domainCatalog,
        IDomainMaintenance domainMaintenance,
        ILogger<DomainController> logger) : base(serverService, configuration, memoryCache)
    {
        _domains = domains;
        _classes = classes;
        _domainCatalog = domainCatalog;
        _domainMaintenance = domainMaintenance;
        _logger = logger;
    }

    /// <summary>Reloads <see cref="DomainCatalog"/> from disk and runs <see cref="IDomainMaintenance.RunAsync"/> (same work as Refiner's domain loop).</summary>
    private async Task<string?> AfterDomainsPersistedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var enabled = await _domains.LoadEnabledDomainsAsync(cancellationToken).ConfigureAwait(false);
            _domainCatalog.Replace(enabled);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not refresh domain catalog after domains.json change.");
        }

        try
        {
            await _domainMaintenance.RunAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Technitium DNS sync failed after domains.json change.");
            return ex.Message;
        }
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
        var dnsErr = await AfterDomainsPersistedAsync(cancellationToken).ConfigureAwait(false);
        TempData[TempDataMessageKey] = dnsErr is null
            ? "Domains saved; DNS/catalog updated."
            : $"Domains saved. Technitium sync failed: {dnsErr}";
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
        var baseMsg = added == 0
            ? "No new domains added (duplicates or empty lines skipped)."
            : $"Added {added} domain(s).";
        if (added > 0)
        {
            var dnsErr = await AfterDomainsPersistedAsync(cancellationToken).ConfigureAwait(false);
            TempData[TempDataMessageKey] = dnsErr is null
                ? baseMsg + " DNS/catalog updated."
                : $"{baseMsg} Technitium sync failed: {dnsErr}";
        }
        else
        {
            TempData[TempDataMessageKey] = baseMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}
