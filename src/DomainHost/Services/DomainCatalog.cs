using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DomainHost.Models;

namespace DomainHost.Services;

public sealed class DomainCatalog : IDomainCatalog
{
    private ImmutableDictionary<string, DomainRecord> _byHost =
        ImmutableDictionary<string, DomainRecord>.Empty;

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

    /// <summary>Turns <c>a.b.c</c> into <c>b.c</c>; returns empty when there is no dot.</summary>
    private static string StripLeftmostLabel(string host)
    {
        var i = host.IndexOf('.');
        return i < 0 ? "" : host[(i + 1)..];
    }

    /// <summary>Lowercase and strip port (if present). Subdomains match via <see cref="TryGetBestMatch"/> suffix walk.</summary>
    private static string NormalizeHost(string host)
    {
        var h = host.Trim();
        var colon = h.IndexOf(':');
        if (colon >= 0)
            h = h[..colon];
        return h.Trim().ToLowerInvariant();
    }
}
