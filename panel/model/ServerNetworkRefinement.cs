using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace model;

/// <summary>Derives <see cref="ServerModel"/> network fields from machine interfaces when unset.</summary>
public static class ServerNetworkRefinement
{
    /// <summary>Up interfaces, unicast IPv4, link-local excluded, de-duplicated in discovery order.</summary>
    static List<string> CollectUniqueUnicastIpv4Strings()
    {
        var unique = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (IsIpv4LinkLocal(ua.Address))
                    continue;
                var s = ua.Address.ToString();
                if (!seen.Add(s))
                    continue;
                unique.Add(s);
            }
        }

        return unique;
    }

    /// <summary>IPv4 for <see cref="ServerModel.ServerIp"/>: public first (discovery order), then private; <c>127.0.0.1</c> only if nothing else exists.</summary>
    public static IReadOnlyList<string> GetOrderedCandidateIpv4Strings()
    {
        var unique = CollectUniqueUnicastIpv4Strings();
        var publicFirst = new List<string>();
        var privateAfter = new List<string>();
        foreach (var s in unique)
        {
            var a = IPAddress.Parse(s);
            if (Dev.IsPrivateIP(a))
                privateAfter.Add(s);
            else
                publicFirst.Add(s);
        }

        var merged = publicFirst.Concat(privateAfter).ToList();
        if (merged.Count == 0)
            merged.Add("127.0.0.1");
        return merged;
    }

    /// <summary>Public (globally routable) IPv4 only, discovery order. Empty when none — no LAN or loopback substitute.</summary>
    public static IReadOnlyList<string> GetPublicOrderedIpv4Strings()
    {
        var unique = CollectUniqueUnicastIpv4Strings();
        var publicOnly = new List<string>();
        foreach (var s in unique)
        {
            if (!Dev.IsPrivateIP(IPAddress.Parse(s)))
                publicOnly.Add(s);
        }

        return publicOnly;
    }

    static bool IsIpv4LinkLocal(IPAddress a) =>
        a.AddressFamily == AddressFamily.InterNetwork &&
        a.ToString().StartsWith("169.", StringComparison.Ordinal);

    /// <summary>Fills <see cref="ServerModel.ServerIp"/> when blank (public then private, else loopback). Fills DNS only from public IPs when blank; otherwise leaves DNS empty.</summary>
    public static void FillIfUnset(ServerModel server)
    {
        var ips = GetOrderedCandidateIpv4Strings();
        var publicIps = GetPublicOrderedIpv4Strings();

        if (string.IsNullOrWhiteSpace(server.ServerIp))
            server.ServerIp = ips[0];

        if (string.IsNullOrWhiteSpace(server.PrimaryDns) && publicIps.Count > 0)
            server.PrimaryDns = publicIps[0];

        if (string.IsNullOrWhiteSpace(server.SecondaryDns) && publicIps.Count >= 2)
            server.SecondaryDns = publicIps[1];
    }
}
