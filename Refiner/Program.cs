using System.Data;
using System.Data.SqlClient;
using model;

namespace Refiner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Killer.StartKilling();
        var server = "";
        var action = "";
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        if (args.Length >= 2)
        {
            action = args[1].Trim();
        }
        Console.WriteLine($"Refiner {server} - {action}");
        var dirs = Directory.GetDirectories(@"C:\data");
        foreach (var dir in dirs)
        {
            try
            {
                var x = new ServerService();
                var serverFile = System.IO.Path.GetFileName(dir);
                if (!(string.IsNullOrEmpty(server) || serverFile == server))
                    continue;
                
                var result = x.RefineServerLite(serverFile);
                if (result.ServerModel.HasToWork)
                {
                    Console.WriteLine($"Foregrounding server: {serverFile}");
                    x.ForegroundServer(serverFile);
                    continue;
                }
                
                Console.WriteLine($"Refining server: {serverFile} - {action}");
                result = x.RefineServer(serverFile, action);
                
                if (result.Exception != null || result.ServerModel == null)
                    continue;
                
                if (string.IsNullOrEmpty(server))
                {
                    Console.WriteLine($"Starting maintaince, {serverFile} ({server}-{action})");

                    try
                    {
                        await UnuIm(result.ServerModel);
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