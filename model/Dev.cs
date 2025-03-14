using System.Net;
using System.Net.NetworkInformation;
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
    public const string ModeDebug = "debug";
    
    public static string[] KnownDomains = new[] { "masterhost.online", "masterhost2.online" };
    public static string[] KnownSubs = new[] {"", "cp", "dev" };
        
    private static string DevIp = "127.0.0.1";
    private static string DevHost = "localhost.masterhost.online:5000";
    private static string DevPassword = "Putin123";

    public static void DefaultServer(string serverName)
    {
        if (!Directory.Exists(ServerModelLoader.RootDataStatic))
            Directory.CreateDirectory(ServerModelLoader.RootDataStatic);
        if (!Directory.Exists(ServerService.ServerDir(serverName)))
            Directory.CreateDirectory(ServerService.ServerDir(serverName));
        if (File.Exists(ServerService.DataFile(serverName)))
            return;
        var server = new ServerModel()
        {
            StartUrls = new List<string>(), StartDownloads = new List<string>(),
            Pushes = new List<string>(), Server = serverName, ServerIp = serverName
        };
        if (serverName == ModeDefault)
        {
            DefaultEndPoint(server);
        }
        if (serverName == ModeDebug)
        {
            DebugEndPoint(server);
        }
        
        File.WriteAllText(  ServerService.DataFile(serverName),
            JsonSerializer.Serialize(server, ServerService.JSO));
    }

    private static void DebugEndPoint(ServerModel server)
    {
        server.Alias = DevHost;
        server.ServerIp = DevIp;
    }

    private static void DefaultEndPoint(ServerModel server)
    {
        var ips = GetPublicIPv4Addresses();
        if (ips.Count > 0)
        {
            var results = new List<(string, string)>();
            foreach (var rootIp in ips)
            {
                foreach (var rootDomain in KnownDomains)
                {
                    foreach (var sub in KnownSubs)
                    {
                        var domain = rootDomain;
                        if (!string.IsNullOrEmpty(sub))
                            domain = sub + '.' + rootDomain;
                        var ip = ResolveDomainToIP(domain);
                        if (rootIp == ip)
                        {
                            results.Add((domain, rootIp));
                        }
                    }
                }
            }
            var result  = results.FirstOrDefault(a=> CountDots(a.Item1) == 2);
            if (string.IsNullOrEmpty(result.Item1))
                result  = results.FirstOrDefault(a=> CountDots(a.Item1) == 1);
            if (!string.IsNullOrEmpty(result.Item1))
            {
                server.Alias = result.Item1;
                server.ServerIp = result.Item2;
            }
        }
        else
        {
            server.Alias = DevHost;
            server.ServerIp = DevHost;
        }
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
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
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
        byte[] bytes = ipAddress.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true, // 10.0.0.0 - 10.255.255.255
            172 => bytes[1] >= 16 && bytes[1] <= 31, // 172.16.0.0 - 172.31.255.255
            192 => bytes[1] == 168, // 192.168.0.0 - 192.168.255.255
            _ => false,
        };
    }
}