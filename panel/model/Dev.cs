using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace model;

public class Dev
{
    public static string Mode
    {
        get
        {
#if DEBUG
            return ModeDebug ;
#else
            return ModeDefault;
#endif
        }
    }

    public const string ModeDefault = "default";
    public const string ModeDebug = "default";

    public static string[] KnownDomains = new[] { "masterhost.online", "masterhost2.online" };
    public static string[] KnownSubs = new[] {"", "cp", "dev" };

    private static string DevIp = "127.0.0.1";
    private static string DevHost = "localhost.masterhost.online:5000";
    private static string DevPassword = "Putin123";

    public static void DefaultServer(string serverName, IPanelServerPaths paths, JsonSerializerOptions jso, string? serverIp = null)
    {
        if (!Directory.Exists(paths.RootData))
            Directory.CreateDirectory(paths.RootData);
        if (!Directory.Exists(paths.ServerDir(serverName)))
            Directory.CreateDirectory(paths.ServerDir(serverName));
        if (File.Exists(paths.DataFile(serverName)))
            return;
        ServerModel server;
        try
        {
            server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(paths.DataFile(serverName)), jso)!;
            server.Paths = paths;
        }
        catch (Exception)
        {
            server = new ServerModel { Server = serverName, Paths = paths };
        }

        if (!string.IsNullOrEmpty(serverIp))
            server.ServerIp = serverIp;

        if (string.IsNullOrEmpty(server.ServerIp))
        {
            var all = GetPublicIPv4Addresses();
            server.ServerIp = all.Count >= 1 ? all[0] : "127.0.0.1";
        }

        File.WriteAllText(paths.DataFile(serverName), JsonSerializer.Serialize(server, jso));
    }

    static int CountDots(string input)
    {
        return input.Split('.').Length - 1;
    }

    private static List<string>? _privateIpAddresses = null;
    public static List<string> GetPublicIPv4Addresses()
    {
        if (_privateIpAddresses != null)
        {
            return _privateIpAddresses;
        }
        List<string> ipv4Addresses = new List<string>();

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) 
                    {
                        if (!IsPrivateIP(ip.Address))
                        {
                            ipv4Addresses.Add(ip.Address.ToString());
                        }
                    }
                }
            }
        }
        _privateIpAddresses = ipv4Addresses;
        if (_privateIpAddresses.Count == 0)
            _privateIpAddresses.Add(LocalNetwork.GetPrimaryIpAddress()!);
        return _privateIpAddresses;
    }



    static string ResolveDomainToIP(string domain)
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(domain);
            return addresses.Length > 0 ? addresses[0].ToString() : "No IP found";
        }
        catch (Exception e)
        {
            return "";
        }
    }

    public static bool IsPrivateIP(IPAddress ipAddress)
    {
        if (ipAddress.ToString() == "127.0.0.1")
            return true;
        if (ipAddress.ToString().StartsWith("169."))
            return true;
        byte[] bytes = ipAddress.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true, 
            172 => bytes[1] >= 16 && bytes[1] <= 31, 
            192 => bytes[1] == 168, 
            _ => false,
        };
    }
}



public static class LocalNetwork
{
        public static string? GetPrimaryIpAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                continue;
            var name = (ni.Name ?? "").ToLowerInvariant();
            var desc = (ni.Description ?? "").ToLowerInvariant();
            if (name.Contains("virtual") || desc.Contains("virtual") ||
                name.Contains("vmware") || desc.Contains("vmware") ||
                name.Contains("vbox") || desc.Contains("virtualbox") ||
                name.Contains("hyper-v") || desc.Contains("hyper-v") ||
                name.Contains("docker") || desc.Contains("docker") ||
                name.Contains("vpn") || desc.Contains("vpn"))
                continue;

            var addr = ni.GetIPProperties().UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(u.Address));
            if (addr != null)
                return addr.Address.ToString();
        }
        return null;
    }
}

