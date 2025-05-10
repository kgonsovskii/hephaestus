using System.DirectoryServices.AccountManagement;
using System.Text.RegularExpressions;
using model;

namespace Cloner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Killer.StartKilling(true);
        
        var server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        var model = ServerModelLoader.LoadServer(server);
        Console.WriteLine($"Cloning server {server}");

        Runner.LogFile = model.UserCloneLog;
        Runner.Server = model.Server;
        Runner.Clean();
        
        Prepare(model);
        
        Runner.RunPsFile("install-copy");
        
        Runner.Kill("SharpRdp.exe");
        Killer.StopKilling();
    }

    public static void Prepare(ServerModel model)
    {
        Runner.Kill("SharpRdp.exe");
        if (Dev.Mode != Dev.ModeDebug)
            Runner.RunPsFile("install-pre", true, false, 0,    new ValueTuple<string, object>("-reboot", "true") );
        
        using (var impersonation = ImpersonationContext.AsRdp())
        {
            impersonation.Run(() =>
            {
                if (Dev.Mode != Dev.ModeDebug)
                    Runner.RunPsFile("install-pre", true, false, 0,    new ValueTuple<string, object>("-reboot", "false") );
                Runner.Kill("SharpRdp.exe");

                Runner.Run(ServerModelLoader.SharpRdpLocal, false, true, 6000,
                    new ValueTuple<string, object>("--server", "localhost"),
                    new ValueTuple<string, object>("--username", "rdp"),
                    new ValueTuple<string, object>("--password", Runner.RdpPassword),
                    new ValueTuple<string, object>("--command", $"\"cls\""));
                Runner.LocalSession = RdpSessionHelper.GetActiveRdpSessionIdForUser("rdp");
                Console.WriteLine($"Local session: {Runner.LocalSession}");
                
                foreach (var c in Commands)
                {
                    Runner.RunIn(ServerModelLoader.SharpRdp, true, true, 60,
                        new ValueTuple<string, object>("--server", model.CloneModel.CloneServerIp),
                        new ValueTuple<string, object>("--username", model.CloneModel.CloneUser),
                        new ValueTuple<string, object>("--password", model.CloneModel.ClonePassword),
                        new ValueTuple<string, object>("--command", $"\"{c}\""));
                }
                Runner.Kill("SharpRdp.exe");
            });
        }
        
        if (Dev.Mode != Dev.ModeDebug)
            Runner.RunPsFile("install-pre", true, false, 0,    new ValueTuple<string, object>("-reboot", "true") ); 
        Runner.Kill("SharpRdp.exe");
    }
    
    public static string[] Commands = new[]
    {
        "Enable-PSRemoting -Force",
        "Set-Service -Name WinRM -StartupType Automatic",
        "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
    };
}