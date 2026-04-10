using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Domain;

public sealed class TechnitiumDnsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TechnitiumDnsClient> _logger;

    public TechnitiumDnsClient(HttpClient http, ILogger<TechnitiumDnsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string> LoginAsync(string user, string password, CancellationToken cancellationToken)
    {
        var url = $"api/user/login?user={Q(user)}&pass={Q(password)}&includeInfo=false";
        using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = doc.RootElement;
        if (!string.Equals(root.GetProperty("status").GetString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Technitium login failed: status not ok.");
        return root.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Technitium login: missing token.");
    }

    /// <summary>
    /// Sets global DNS forwarders for the Technitium server (<c>api/settings/set</c>).
    /// A/AAAA record TTL is not set here; omit <c>ttl</c> on records so Technitium uses <c>defaultRecordTtl</c> from server settings.
    /// </summary>
    public async Task ApplyGlobalForwardersAsync(string token, TechnitiumOptions options, CancellationToken cancellationToken)
    {
        if (!options.ForwarderEnabled)
            return;
        var list = (options.Forwarders ?? "").Trim();
        if (list.Length == 0)
            return;
        var url = $"api/settings/set?token={Q(token)}&forwarders={Q(list)}";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Technitium global forwarders set to {Forwarders}", list);
    }

    /// <summary>Sets Technitium <c>recursion</c> (<c>api/settings/set</c>), e.g. <c>Allow</c> for all networks.</summary>
    public async Task ApplyRecursionPolicyAsync(string token, TechnitiumOptions options, CancellationToken cancellationToken)
    {
        var policy = (options.Recursion ?? "").Trim();
        if (policy.Length == 0)
            return;
        var url = $"api/settings/set?token={Q(token)}&recursion={Q(policy)}";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Technitium recursion policy set to {Recursion}", policy);
    }

    public async Task<IReadOnlyList<TechnitiumZoneSnapshot>> ListZonesAsync(string token, CancellationToken cancellationToken)
    {
        var url = $"api/zones/list?token={Q(token)}";
        using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var zones = doc.RootElement.GetProperty("response").GetProperty("zones");
        var list = new List<TechnitiumZoneSnapshot>();
        foreach (var z in zones.EnumerateArray())
        {
            if (!z.TryGetProperty("name", out var nameEl))
                continue;
            var name = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var type = "";
            if (z.TryGetProperty("type", out var typeEl))
                type = typeEl.GetString() ?? "";
            var internalZone = z.TryGetProperty("internal", out var intEl) && intEl.ValueKind == JsonValueKind.True;
            var dnssec = "";
            if (z.TryGetProperty("dnssecStatus", out var dsEl))
                dnssec = dsEl.GetString() ?? "";
            list.Add(new TechnitiumZoneSnapshot
            {
                Name = name.Trim(),
                Type = type,
                Internal = internalZone,
                DnssecStatus = dnssec
            });
        }

        return list;
    }

    public async Task CreatePrimaryZoneAsync(string token, string zoneName, CancellationToken cancellationToken)
    {
        var url = $"api/zones/create?token={Q(token)}&zone={Q(zoneName)}&type=Primary";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Technitium created primary zone {Zone}", zoneName);
    }

    public async Task DeleteZoneAsync(string token, string zoneName, CancellationToken cancellationToken)
    {
        var url = $"api/zones/delete?token={Q(token)}&zone={Q(zoneName)}";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Technitium deleted zone {Zone}", zoneName);
    }

    /// <summary>DNSSEC-sign a primary zone (Technitium <c>/api/zones/dnssec/sign</c>).</summary>
    public async Task SignPrimaryZoneAsync(string token, string zoneName, TechnitiumOptions options, CancellationToken cancellationToken)
    {
        var o = options;
        var url =
            $"api/zones/dnssec/sign?token={Q(token)}&zone={Q(zoneName)}" +
            $"&algorithm={Q(o.DnssecSignAlgorithm)}&dnsKeyTtl={o.DnssecDnsKeyTtl}&zskRolloverDays={o.DnssecZskRolloverDays}" +
            $"&nxProof={Q(o.DnssecNxProof)}&iterations={o.DnssecNsec3Iterations}&saltLength={o.DnssecNsec3SaltLength}" +
            $"&curve={Q(o.DnssecCurve)}";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Technitium DNSSEC-signed zone {Zone}", zoneName);
    }

    public static bool IsDnssecUnsigned(string? dnssecStatus) =>
        string.IsNullOrWhiteSpace(dnssecStatus) ||
        string.Equals(dnssecStatus, "Unsigned", StringComparison.OrdinalIgnoreCase);

    public static string? FindZoneForFqdn(string fqdn, IEnumerable<string> zoneNames)
    {
        var lower = fqdn.TrimEnd('.').ToLowerInvariant();
        foreach (var z in zoneNames.OrderByDescending(x => x.Length))
        {
            var zz = z.TrimEnd('.').ToLowerInvariant();
            if (lower == zz)
                return z;
            if (lower.EndsWith("." + zz, StringComparison.Ordinal))
                return z;
        }

        return null;
    }

    public async Task SyncAaaaAsync(
        string token,
        string fqdn,
        string zone,
        IPAddress? desiredV4,
        IPAddress? desiredV6,
        bool ptrAndReverseZone,
        CancellationToken cancellationToken)
    {
        await SyncOneAsync(token, fqdn, zone, "A", desiredV4?.ToString(), ptrAndReverseZone, cancellationToken)
            .ConfigureAwait(false);
        await SyncOneAsync(token, fqdn, zone, "AAAA", desiredV6?.ToString(), ptrAndReverseZone, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SyncOneAsync(
        string token,
        string fqdn,
        string zone,
        string type,
        string? desiredIp,
        bool ptrAndReverseZone,
        CancellationToken cancellationToken)
    {
        var ptrQs = ptrAndReverseZone ? "&ptr=true&createPtrZone=true" : "";

        var existing = await GetExistingIpAsync(token, fqdn, zone, type, cancellationToken).ConfigureAwait(false);
        if (desiredIp == null)
        {
            if (existing != null)
                await DeleteRecordAsync(token, fqdn, zone, type, existing, cancellationToken).ConfigureAwait(false);
            else if (string.Equals(type, "AAAA", StringComparison.OrdinalIgnoreCase))
                _logger.LogInformation(
                    "Technitium AAAA not set for {Domain}: no IPv6 target (add v6 in domains.json ip or ensure a routable IPv6 via NetworkAddressPreference when ip is empty)",
                    fqdn);
            return;
        }

        if (string.Equals(existing, desiredIp, StringComparison.OrdinalIgnoreCase))
            return;

        if (existing != null)
        {
            var upd =
                $"api/zones/records/update?token={Q(token)}&domain={Q(fqdn)}&zone={Q(zone)}&type={Q(type)}" +
                $"&ipAddress={Q(existing)}&newIpAddress={Q(desiredIp)}" + ptrQs;
            await ApiGetOkAsync(upd, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Technitium updated {Type} {Domain} -> {Ip}", type, fqdn, desiredIp);
        }
        else
        {
            var add =
                $"api/zones/records/add?token={Q(token)}&domain={Q(fqdn)}&zone={Q(zone)}&type={Q(type)}" +
                $"&ipAddress={Q(desiredIp)}&overwrite=true" + ptrQs;
            await ApiGetOkAsync(add, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Technitium added {Type} {Domain} -> {Ip}", type, fqdn, desiredIp);
        }
    }

    private async Task<string?> GetExistingIpAsync(
        string token,
        string fqdn,
        string zone,
        string type,
        CancellationToken cancellationToken)
    {
        var url =
            $"api/zones/records/get?token={Q(token)}&domain={Q(fqdn)}&zone={Q(zone)}&listZone=false";
        using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("response", out var response))
            return null;
        if (!response.TryGetProperty("records", out var records))
            return null;
        foreach (var rec in records.EnumerateArray())
        {
            if (!rec.TryGetProperty("type", out var t) || !string.Equals(t.GetString(), type, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!rec.TryGetProperty("rData", out var rdata))
                continue;
            if (rdata.TryGetProperty("ipAddress", out var ipEl))
            {
                return ipEl.GetString();
            }
        }

        return null;
    }

    private async Task DeleteRecordAsync(
        string token,
        string fqdn,
        string zone,
        string type,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var url =
            $"api/zones/records/delete?token={Q(token)}&domain={Q(fqdn)}&zone={Q(zone)}&type={Q(type)}&ipAddress={Q(ipAddress)}";
        await ApiGetOkAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Technitium deleted {Type} {Domain} ({Ip})", type, fqdn, ipAddress);
    }

    private async Task ApiGetOkAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Technitium API HTTP {(int)resp.StatusCode}: {body}");
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("status", out var st) ||
            !string.Equals(st.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Technitium API error: {body}");
    }

    private static string Q(string? value) => Uri.EscapeDataString(value ?? "");
}
