using System.Net;
using System.Net.Sockets;

namespace Commons;

public static class ClientIpNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        var comma = trimmed.IndexOf(',');
        if (comma >= 0)
            trimmed = trimmed[..comma].Trim();
        if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return "";
        if (!IPAddress.TryParse(trimmed, out var ip))
            return trimmed.Length <= 45 ? trimmed : trimmed[..45];
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        return ip.ToString();
    }

    public static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return false;
        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}
