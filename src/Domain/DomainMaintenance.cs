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
/// Skips names in <c>domains-ignore.json</c>. Does not delete Technitium <c>internal</c> zones.
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
            _logger.LogDebug("Technitium sync skipped (Technitium:Enabled is false).");
            return;
        }

        var hostOpts = _hostOptions.Value;
        var ignoreName = hostOpts.DomainsIgnoreFileName.Trim();
        if (ignoreName.Length == 0)
            ignoreName = "domains-ignore.json";
        var ignorePath = Path.Combine(_webPaths.WebRootFullPath, ignoreName);
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

            ParseTargetAddresses(row.Ip, out var v4, out var v6);
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

    private static void ParseTargetAddresses(string? ipField, out IPAddress? v4, out IPAddress? v6)
    {
        v4 = null;
        v6 = null;
        if (string.IsNullOrWhiteSpace(ipField))
        {
            NetworkAddressPreference.TryGetPreferredAddresses(out v4, out v6);
            return;
        }

        foreach (var part in ipField.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Length == 0)
                continue;
            if (!IPAddress.TryParse(t, out var ip))
                continue;
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                v4 = ip;
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                v6 = ip;
        }

        // Explicit list may include only IPv4; still publish AAAA using this host's preferred IPv6 when available.
        if (v6 == null)
        {
            NetworkAddressPreference.TryGetPreferredAddresses(out _, out var preferredV6);
            v6 = preferredV6;
        }
    }
}
