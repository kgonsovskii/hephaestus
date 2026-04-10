using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Domain;

public interface IDomainCatalog
{
    bool TryGetBestMatch(string host, [NotNullWhen(true)] out DomainRecord? record);
}

public sealed class DomainCatalog : IDomainCatalog
{
    private readonly ILogger<DomainCatalog> _logger;
    private ImmutableDictionary<string, DomainRecord> _byHost =
        ImmutableDictionary<string, DomainRecord>.Empty;

    public DomainCatalog(ILogger<DomainCatalog> logger)
    {
        _logger = logger;
    }

    public void Replace(IEnumerable<DomainRecord> records)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in records)
        {
            if (!r.Enabled)
                continue;
            var key = NormalizeHost(r.Domain);
            if (key.Length == 0)
                continue;
            builder[key] = r;
        }

        _byHost = builder.ToImmutable();

        foreach (var r in builder.Values.OrderBy(x => x.Domain, StringComparer.OrdinalIgnoreCase))
        {
            DomainIpFieldParser.ParseTargetAddresses(r.Ip, out var v4, out var v6);
            _logger.LogInformation(
                "Domain catalog entry {Domain}: ip={Ip} ipv4={Ipv4} ipv6={Ipv6}",
                r.Domain,
                string.IsNullOrWhiteSpace(r.Ip) ? "(default)" : r.Ip.Trim(),
                v4?.ToString() ?? "-",
                v6?.ToString() ?? "-");
        }
    }

    public bool TryGetBestMatch(string host, [NotNullWhen(true)] out DomainRecord? record)
    {
        record = null;
        var key = NormalizeHost(host);
        while (!string.IsNullOrEmpty(key))
        {
            if (_byHost.TryGetValue(key, out var match))
            {
                record = match;
                return true;
            }

            key = StripLeftmostLabel(key);
        }

        return false;
    }

    private static string StripLeftmostLabel(string host)
    {
        var i = host.IndexOf('.');
        return i < 0 ? "" : host[(i + 1)..];
    }

    private static string NormalizeHost(string host)
    {
        var h = host.Trim();
        var colon = h.IndexOf(':');
        if (colon >= 0)
            h = h[..colon];
        return h.Trim().ToLowerInvariant();
    }
}
