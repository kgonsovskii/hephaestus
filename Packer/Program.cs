using model;

namespace Packer;

internal static class Program
{
    private static void Log(string s)
    {
        Console.WriteLine(s);
    }
    private static async Task Main(string[] args)
    {
        Killer.StartKilling();
        var server = "default";
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        var x = new ServerService();
        x.PackServer(server, "empty", Log);
        Killer.StopKilling();
    }
}