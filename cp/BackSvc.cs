using System.Net;
using System.Net.NetworkInformation;
using model;

namespace cp;

public class BackSvc: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await DoWork();
        }
        catch (Exception e)
        {
       
        }
        await Task.Delay(1 * 1000 * 60 * 7, stoppingToken);
    }

    public static Dictionary<string, string> Map = new Dictionary<string, string >();
    
    public static List<string> Ips = new List<string>();

    public static string EvalServer(HttpRequest request)
    {
        if (request.Headers.TryGetValue("HTTP_X_SERVER", out Microsoft.Extensions.Primitives.StringValues value))
        {
            var serverFor = value.First();
            var server = serverFor.Split(',').Select(s => s.Trim()).FirstOrDefault().Trim();
            return server;
        }
        
        if (request.Host.Host == "localhost")
            return ServerModelLoader.ipFromHost(ServerModelLoader.DomainControllerStatic);

        return ServerModelLoader.ipFromHost(request.Host.Host);
    }
    
    public static async Task DoWork()
    {
        var dirs = System.IO.Directory.GetDirectories(@"C:\data");
        var result = new Dictionary<string, string>();
        var ips = new List<string>();
        foreach (var dir in dirs)
        {
            var x = new ServerService();
            var serverFile = System.IO.Path.GetFileName(dir);
            var a = x.GetServer(serverFile, false).ServerModel!;
            result.Add(a.Alias, a.Server);
            //ips.AddRange(a.Interfaces);
            //ips.Add(a.Server);
        }
        Map = result;
        //ips.Add("127.0.0.1");
        //ips.Add("::1");
        Ips = ips.Distinct().ToList();
    }
    
    public static List<string> GetPublicIPv4Addresses()
    {
        List<string> ipv4Addresses = new List<string>();

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
                    {
                        // Filter out private IP ranges
                        if (!IsPrivateIP(ip.Address))
                        {
                            ipv4Addresses.Add(ip.Address.ToString());
                        }
                    }
                }
            }
        }

        return ipv4Addresses;
    }
    
    public static bool IsIpAllowed(string remoteIp)
    {
        foreach (var range in Ips)
        {
            if (range == remoteIp.ToString())
            {
                return true;
            }
        }
        return false;
    }
    
    

    public static bool IsPrivateIP(IPAddress ipAddress)
    {
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