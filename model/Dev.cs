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

    public static void DefaultServer(string serverName, string? serverIp = null)
    {
        if (!Directory.Exists(ServerModelLoader.RootDataStatic))
            Directory.CreateDirectory(ServerModelLoader.RootDataStatic);
        if (!Directory.Exists(ServerService.ServerDir(serverName)))
            Directory.CreateDirectory(ServerService.ServerDir(serverName));
        if (File.Exists(ServerService.DataFile(serverName)))
            return;
        ServerModel server = null;
        try
        {
            server = ServerModelLoader.LoadServerFileInternal(ServerService.DataFile(serverName));
        }
        catch (Exception e)
        {
            server = new ServerModel() { Server = serverName };
        }
        if (!string.IsNullOrEmpty(serverIp))
        {
            server.ServerIp = serverIp;
        }
        ServerModelLoader.SaveServer(serverName, server);
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
        if (ipAddress.ToString().StartsWith("169."))
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