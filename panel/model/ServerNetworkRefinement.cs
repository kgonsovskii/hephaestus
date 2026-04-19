using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace model;

/// <summary>Derives <see cref="ServerModel"/> network fields from machine interfaces when unset: public IPv4 first, then LAN.</summary>
public static class ServerNetworkRefinement
{
    /// <summary>IPv4 addresses, de-duplicated: all non-private first (in discovery order), then private (same order).</summary>
    public static IReadOnlyList<string> GetOrderedCandidateIpv4Strings()
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

    static bool IsIpv4LinkLocal(IPAddress a) =>
        a.AddressFamily == AddressFamily.InterNetwork &&
        a.ToString().StartsWith("169.", StringComparison.Ordinal);

    /// <summary>Fills <see cref="ServerModel.ServerIp"/>, <see cref="ServerModel.PrimaryDns"/>, <see cref="ServerModel.SecondaryDns"/> only when blank.</summary>
    public static void FillIfUnset(ServerModel server)
    {
        var ips = GetOrderedCandidateIpv4Strings();

        if (string.IsNullOrWhiteSpace(server.ServerIp))
            server.ServerIp = ips[0];

        if (string.IsNullOrWhiteSpace(server.PrimaryDns))
            server.PrimaryDns = ips[0];

        if (string.IsNullOrWhiteSpace(server.SecondaryDns) && ips.Count >= 2)
            server.SecondaryDns = ips[1];
    }
}
