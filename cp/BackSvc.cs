using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using model;

namespace cp;

public class BackSvc: BackgroundService
{
    internal static void Initialize()
    {
        Dev.DefaultServer(Dev.Mode);
        DoWork();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            DoWork();
        }
        catch (Exception e)
        {
       
        }
        await Task.Delay(1 * 1000 * 60 * 7, stoppingToken);
    }

    public static Dictionary<string, string> Servers = new Dictionary<string, string >();
    private static List<string> Ips;

    public static string EvalServer(HttpRequest request)
    {
        if (request.Headers.TryGetValue("HTTP_X_SERVER", out Microsoft.Extensions.Primitives.StringValues value))
        {
            var serverFor = value.First();
            var server = serverFor.Split(',').Select(s => s.Trim()).FirstOrDefault().Trim();
            return server;
        }
        return Dev.Mode;
    }

    private static void DoWork()
    {
        try
        {
            var dirs = System.IO.Directory.GetDirectories(ServerModelLoader.RootDataStatic);
            var result = new Dictionary<string, string>();
            var ips = new List<string>();
            foreach (var dir in dirs)
            {
                var x = new ServerService();
                var serverFile = System.IO.Path.GetFileName(dir);
                var a = x.GetServer(serverFile, false, ServerService.Get.RaiseError).ServerModel!;
                result.Add(a.Server, a.Alias);
            }
            Servers = result;
            Ips = Servers.Values.ToList();
            Ips.AddRange(GetPublicIPv4Addresses());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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
    public static List<string> GetPublicIPv4Addresses() => Dev.GetPublicIPv4Addresses();

}