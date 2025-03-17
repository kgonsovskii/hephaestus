using System.Data;
using model;

namespace Cloner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Dev.DefaultServer(args.Length > 0 ? args[0] : Dev.Mode);
        Killer.StartKilling();
        var server = "debug";
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }

        var x = new ServerService();
        x.CloneServer(server);
        Killer.StopKilling();
    }
}