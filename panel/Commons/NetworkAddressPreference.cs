using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Commons;

public static class NetworkAddressPreference
{
        public static void TryGetPreferredAddresses(out IPAddress? ipv4, out IPAddress? ipv6)
    {
        ipv4 = null;
        ipv6 = null;
        var v4Public = new List<IPAddress>();
        var v4Private = new List<IPAddress>();
        var v6Global = new List<IPAddress>();
        var v6Ula = new List<IPAddress>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;
            if (ShouldSkipInterface(ni.Name, ni.Description))
                continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                var addr = ua.Address;
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (IsIpv4PrivateOrSpecial(addr))
                        v4Private.Add(addr);
                    else
                        v4Public.Add(addr);
                }
                else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (addr.IsIPv6LinkLocal || addr.IsIPv6Multicast)
                        continue;
                    if (IsIpv6UniqueLocal(addr))
                        v6Ula.Add(addr);
                    else if (IsIpv6GlobalUnicast(addr))
                        v6Global.Add(addr);
                }
            }
        }

        ipv4 = v4Public.Count > 0 ? v4Public[0] : v4Private.Count > 0 ? v4Private[0] : null;
        ipv6 = v6Global.Count > 0 ? v6Global[0] : v6Ula.Count > 0 ? v6Ula[0] : null;
    }

    private static bool ShouldSkipInterface(string name, string description)
    {
        var d = description + " " + name;
        if (string.IsNullOrEmpty(d))
            return false;
        d = d.ToUpperInvariant();
        string[] bad =
        [
            "LOOPBACK", "VMWARE", "VIRTUALBOX", "HYPER-V", "VIRTUAL ", " TAP", "TUN", "WSL",
            "VETHERNET", "BLUETOOTH", "NPCAP", "VPN", "TAILSCALE", "ZERO TIER", "ZEROTIER",
            "NORDLYNX", "WIREGUARD", "DOCKER", "VBOX", "PANGP", "NETMON", "FILTER"
        ];
        return bad.Any(x => d.Contains(x, StringComparison.Ordinal));
    }

    private static bool IsIpv4PrivateOrSpecial(IPAddress a)
    {
        var bytes = a.GetAddressBytes();
        if (bytes.Length != 4)
            return true;
        if (bytes[0] == 10)
            return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;
        if (bytes[0] == 192 && bytes[1] == 168)
            return true;
        if (bytes[0] == 127)
            return true;
        if (bytes[0] == 169 && bytes[1] == 254)
            return true;
        return false;
    }

    public static bool IsIpv6UniqueLocal(IPAddress a)
    {
        var s = a.ToString();
        return s.StartsWith("fd", StringComparison.OrdinalIgnoreCase) || s.StartsWith("fc", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIpv6GlobalUnicast(IPAddress a)
    {
        
        var bytes = a.GetAddressBytes();
        if (bytes.Length != 16)
            return false;
        var high = bytes[0];
        return (high & 0xE0) == 0x20;
    }
}
