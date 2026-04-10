using System.Net;
using System.Net.Sockets;
using Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Domain;

public interface IDomainMaintenance : IMaintenance
{
}

/// <summary>
/// Aligns Technitium with <c>domains.json</c>: creates/deletes <b>Primary</b> zones to match enabled rows (minus ignore list),
/// optionally applies global DNS forwarders and recursion policy, then sets A/AAAA at each name (TTL from Technitium default record TTL in Settings).
/// For each zone, also ensures the zone apex and a <c>*.zone</c> wildcard get A/AAAA (same targets as the canonical row for that zone; apex row preferred when present),
/// then applies per-row records so explicit hostnames can override. Wildcard sync does not request PTR (not meaningful for <c>*</c>).
/// Skips names in <c>domains-ignore.json</c> (next to <c>domains.json</c> under the Hephaestus data root). Does not delete Technitium <c>internal</c> zones.
/// When a domain has no <c>ip</c>, uses <see cref="NetworkAddressPreference"/> for both v4 and v6.
/// When <c>ip</c> lists only IPv4, still fills IPv6 from <see cref="NetworkAddressPreference"/> so AAAA can be published.
/// </summary>
public sealed class DomainMaintenance : IDomainMaintenance
{
    private readonly IDomainRepository _domains;
    private readonly IWebContentPathProvider _webPaths;
    private readonly IOptionsMonitor<TechnitiumOptions> _technitium;
    private readonly IOptions<DomainHostOptions> _hostOptions;
    private readonly TechnitiumDnsClient _dns;
    private readonly ILogger<DomainMaintenance> _logger;

    public DomainMaintenance(
        IDomainRepository domains,
        IWebContentPathProvider webPaths,
        IOptionsMonitor<TechnitiumOptions> technitium,
        IOptions<DomainHostOptions> hostOptions,
        TechnitiumDnsClient dns,
        ILogger<DomainMaintenance> logger)
    {
        _domains = domains;
        _webPaths = webPaths;
        _technitium = technitium;
        _hostOptions = hostOptions;
        _dns = dns;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var opts = _technitium.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogWarning(
                "Technitium DNS sync skipped (Technitium:Enabled is false). " +
                "If you expect DNS updates, ensure appsettings.json is loaded (DomainHost merges the copy under the exe / BaseDirectory) and Technitium:Enabled is true.");
            return;
        }

        _logger.LogInformation("Technitium DNS sync starting (BaseUrl={BaseUrl}).", opts.BaseUrl.Trim());

        var hostOpts = _hostOptions.Value;
        var ignoreName = hostOpts.DomainsIgnoreFileName.Trim();
        if (ignoreName.Length == 0)
            ignoreName = "domains-ignore.json";
        var ignorePath = Path.Combine(_webPaths.DataRootFullPath, ignoreName);
        if (!File.Exists(ignorePath))
            throw new FileNotFoundException($"domains-ignore file not found: {ignorePath}");

        var ignored = await DomainsIgnoreFile.LoadAsync(ignorePath, cancellationToken).ConfigureAwait(false);
        var records = await _domains.LoadEnabledDomainsAsync(cancellationToken).ConfigureAwait(false);

        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in records)
        {
            var fqdn = row.Domain.Trim();
            if (fqdn.Length == 0 || ignored.Contains(fqdn))
                continue;
            desired.Add(fqdn);
        }

        var token = await _dns.LoginAsync(opts.User, opts.Password, cancellationToken).ConfigureAwait(false);

        try
        {
            await _dns.ApplyGlobalForwardersAsync(token, opts, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set Technitium global DNS forwarders.");
        }

        try
        {
            await _dns.ApplyRecursionPolicyAsync(token, opts, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set Technitium recursion policy.");
        }

        var zoneSnapshots = await _dns.ListZonesAsync(token, cancellationToken).ConfigureAwait(false);
        var zoneNames = new HashSet<string>(zoneSnapshots.Select(z => z.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var z in zoneSnapshots)
        {
            if (!string.Equals(z.Type, "Primary", StringComparison.OrdinalIgnoreCase))
                continue;
            if (z.Internal)
                continue;
            if (ignored.Contains(z.Name))
                continue;
            if (desired.Contains(z.Name))
                continue;
            await _dns.DeleteZoneAsync(token, z.Name, cancellationToken).ConfigureAwait(false);
            zoneNames.Remove(z.Name);
        }

        foreach (var name in desired)
        {
            if (zoneNames.Contains(name))
                continue;
            try
            {
                await _dns.CreatePrimaryZoneAsync(token, name, cancellationToken).ConfigureAwait(false);
                zoneNames.Add(name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create Technitium zone {Zone}; record sync may fail.", name);
            }
        }

        zoneSnapshots = await _dns.ListZonesAsync(token, cancellationToken).ConfigureAwait(false);
        zoneNames = new HashSet<string>(
            zoneSnapshots
                .Where(z => string.Equals(z.Type, "Primary", StringComparison.OrdinalIgnoreCase))
                .Select(z => z.Name),
            StringComparer.OrdinalIgnoreCase);

        if (opts.DnssecEnabled)
        {
            foreach (var name in desired)
            {
                var snap = zoneSnapshots.FirstOrDefault(z =>
                    string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase));
                if (snap == null)
                    continue;
                if (!string.Equals(snap.Type, "Primary", StringComparison.OrdinalIgnoreCase) || snap.Internal)
                    continue;
                if (!TechnitiumDnsClient.IsDnssecUnsigned(snap.DnssecStatus))
                    continue;
                try
                {
                    await _dns.SignPrimaryZoneAsync(token, name, opts, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not DNSSEC-sign Technitium zone {Zone}.", name);
                }
            }
        }

        // Apex + wildcard per zone so every subdomain resolves without listing each host (explicit rows below may override).
        var canonicalByZone = new Dictionary<string, Domain.Models.DomainRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in records)
        {
            var fqdn = row.Domain.Trim();
            if (fqdn.Length == 0 || ignored.Contains(fqdn))
                continue;
            var zone = TechnitiumDnsClient.FindZoneForFqdn(fqdn, zoneNames);
            if (zone == null)
                continue;
            if (!canonicalByZone.TryGetValue(zone, out var existing))
            {
                canonicalByZone[zone] = row;
                continue;
            }

            if (string.Equals(fqdn.TrimEnd('.'), zone.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                canonicalByZone[zone] = row;
        }

        foreach (var pair in canonicalByZone)
        {
            var zone = pair.Key;
            var row = pair.Value;
            DomainIpFieldParser.ParseTargetAddresses(row.Ip, out var v4, out var v6);
            var apexFqdn = zone.TrimEnd('.');
            var wildcardFqdn = TechnitiumDnsClient.WildcardFqdn(zone);
            _logger.LogInformation(
                "Technitium DNS sync zone baseline {Zone}: apex {Apex} + wildcard {Wildcard} IPv4={Ipv4} IPv6={Ipv6}",
                zone,
                apexFqdn,
                wildcardFqdn,
                v4?.ToString() ?? "-",
                v6?.ToString() ?? "-");
            await _dns.SyncAaaaAsync(token, apexFqdn, zone, v4, v6, opts.PtrEnabled, cancellationToken)
                .ConfigureAwait(false);
            await _dns.SyncAaaaAsync(token, wildcardFqdn, zone, v4, v6, ptrAndReverseZone: false, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var row in records)
        {
            var fqdn = row.Domain.Trim();
            if (fqdn.Length == 0)
                continue;
            if (ignored.Contains(fqdn))
            {
                _logger.LogDebug("Skip DNS sync (ignored): {Domain}", fqdn);
                continue;
            }

            var zone = TechnitiumDnsClient.FindZoneForFqdn(fqdn, zoneNames);
            if (zone == null)
            {
                _logger.LogWarning("No Technitium zone hosts {Domain}; skipping records.", fqdn);
                continue;
            }

            DomainIpFieldParser.ParseTargetAddresses(row.Ip, out var v4, out var v6);
            _logger.LogInformation(
                "Technitium DNS sync {Domain}: IPv4={Ipv4} IPv6={Ipv6} zone={Zone} (AAAA is written only when IPv6 is set)",
                fqdn,
                v4?.ToString() ?? "-",
                v6?.ToString() ?? "-",
                zone);
            await _dns.SyncAaaaAsync(token, fqdn, zone, v4, v6, opts.PtrEnabled, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
