using model;

namespace Cloner;

internal static class Program
{
    private static void Log(string s)
    {
        Console.WriteLine(s);
    }
    
    private static async Task Main(string[] args)
    {
        Killer.StartKilling(true);
        var server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        Console.WriteLine($"Cloning server {server}");
        Thread.Sleep(100);

        var x = new ServerService();
        x.CloneServer(server, Log);
        Killer.StopKilling();
    }
}