using model;

namespace CertTool;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }

        var model = ServerModelLoader.LoadServer(server);
        Console.WriteLine($"Certificating server {server}");
        GitHelper.CloneAndCheckout(new WriterX(), Settings.CertRepo, model.CertDir);
    }
}