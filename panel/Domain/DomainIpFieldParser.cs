using System.Net;
using System.Net.Sockets;
using Commons;

namespace Domain;

/// <summary>Parses <c>domains.json</c> <c>ip</c> the same way as <see cref="DomainMaintenance"/> (Technitium sync).</summary>
public static class DomainIpFieldParser
{
    public static void ParseTargetAddresses(string? ipField, out IPAddress? v4, out IPAddress? v6)
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

        if (v6 == null)
        {
            NetworkAddressPreference.TryGetPreferredAddresses(out _, out var preferredV6);
            v6 = preferredV6;
        }
    }
}
