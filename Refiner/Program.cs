using System.Data;
using System.Data.SqlClient;
using model;

namespace Refiner;

internal static class Program
{
    private static void Log(string s)
    {
        Console.WriteLine(s);
    }
    
    private static async Task Main(string[] args)
    {
        Console.WriteLine($"Version=" + VersionFetcher.Version());
   
        Killer.StartKilling(false);
        var server = "";
        var action = "";
        var forceIp = "";
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        if (args.Length >= 2)
        {
            action = args[1].Trim();
        }
        if (args.Length >= 3)
        {
            forceIp = args[2].Trim();
        }
        Dev.DefaultServer(args.Length > 0 ? args[0] : Dev.Mode, forceIp);
        if (!string.IsNullOrEmpty(server))
        {
            Console.WriteLine($"Working direct server: {server}, {action}");
            var x = new ServerService();
            var result = ServerService.GetServerLite(server);
            result.PostModel.Operation = action;
            ServerService.SaveServerLite(server, result);
            x.PostServerAction(server, result, Log);
            return;
        }
        Console.WriteLine($"Refining...");
        var dirs = Directory.GetDirectories(@"C:\data");
        foreach (var dir in dirs)
        {
            try
            {
                var x = new ServerService();
                var serverFile = Path.GetFileName(dir);
  
                var result = ServerService.GetServerLite(serverFile);
                Console.WriteLine($"Starting maintaince: {serverFile}");
                x.PostServerAction(serverFile, result, Log);
 
                try
                {
                    await UnuIm(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                try
                {
                    await StatsJob();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                try
                {
                    await DbJob();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }
        Killer.StopKilling();
    }

    private static async Task UnuIm(ServerModel serverModel)
    {
        try
        {
            //unu.im
            var unuSettings = serverModel.Bux.First(a => a.Id == "unu.im");
            if (unuSettings.Enabled)
            {
                var unu = new UnuIm(unuSettings);
                await unu.Process();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task DbJob()
    {
        await using var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=hephaestus;Trusted_Connection=True;TrustServerCertificate=True;");
        await connection.OpenAsync();
        await using var command = new SqlCommand("dbo.Clean", connection);
        command.CommandType = CommandType.StoredProcedure;
        await command.ExecuteNonQueryAsync();
    }
    
    private static async Task StatsJob()
    {
        await using var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=hephaestus;Trusted_Connection=True;TrustServerCertificate=True;");
        await connection.OpenAsync();
        await using var command = new SqlCommand("dbo.CalcStats", connection);
        command.CommandType = CommandType.StoredProcedure;
        await command.ExecuteNonQueryAsync();
    }
    
}