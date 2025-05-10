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

        if (NetworkUtils.Test(model.CloneModel.CloneServerIp))
        {
            Runner.Log($"No preparation for CloneServerIp {model.CloneModel.CloneServerIp}");
        }
        else
        {
            Runner.Log($"Do preparation for CloneServerIp {model.CloneModel.CloneServerIp}");
            Prepare(model);
        }

        Runner.RunPsFile("install-copy");

        Install(model);
        
        Runner.RunPsFile("publish");

        Runner.CloseSession();
        Killer.StopKilling();

        Runner.Log("ACCOMPLISHED.");
    }

    public static void Prepare(ServerModel model)
    {
        string[] Commands = new[]
        {
            "Enable-PSRemoting -Force",
            "Set-Service -Name WinRM -StartupType Automatic",
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985",
            "Start-Service -Name WinRM"
        };

        Runner.CloseSession();
        Runner.RunPsFile("install-pre", true, false, 0, new ValueTuple<string, object>("-reboot", "true"));

        using (var impersonation = ImpersonationContext.AsRdp())
        {
            impersonation.Run(() =>
            {
                Runner.RunPsFile("install-pre", true, false, 0, new ValueTuple<string, object>("-reboot", "false"));

                Runner.OpenSession();

                foreach (var c in Commands)
                {
                    var cmd = c + "; Set-Content -Path 'C:\\Install\\tag.txt' -Value '$tag'";
                    Runner.RunIn(ServerModelLoader.SharpRdp, true, true, 60,
                        new ValueTuple<string, object>("--server", model.CloneModel.CloneServerIp),
                        new ValueTuple<string, object>("--username", model.CloneModel.CloneUser),
                        new ValueTuple<string, object>("--password", model.CloneModel.ClonePassword),
                        new ValueTuple<string, object>("--command", $"\"{cmd}\""));
                }

                Runner.CloseSession();
            });
        }
        
        Runner.RunPsFile("install-reboot", true, false, 180);

        Runner.CloseSession();
    }


    public static void Install(ServerModel model)
    {
        string[] files = new[]
        {
            "install0",
            "installSql",
            "installSqlTools",
            "installWeb",
            "installWeb2",
            "installTrigger"
        };

        Runner.CloseSession();

        using (var impersonation = ImpersonationContext.AsRdp())
        {
            impersonation.Run(() => { Runner.OpenSession(); });
        }

        foreach (var file in files)
        {
            Runner.RunPsFile("install-reboot", true, false, 120);
            
            using (var impersonation = ImpersonationContext.AsRdp())
            {
                impersonation.Run(() =>
                {
                    var cmd = $". 'C:\\Install\\{file}.ps1; Set-Content -Path 'C:\\Install\\tag.txt' -Value '$tag'";

                    Runner.RunIn(ServerModelLoader.SharpRdp, true, true, 6000,
                        new ValueTuple<string, object>("--server", model.CloneModel.CloneServerIp),
                        new ValueTuple<string, object>("--username", model.CloneModel.CloneUser),
                        new ValueTuple<string, object>("--password", model.CloneModel.ClonePassword),
                        new ValueTuple<string, object>("--command", $"\"{cmd}\""));
                });
            }
        }

        using (var impersonation = ImpersonationContext.AsRdp())
        {
            impersonation.Run(() => { Runner.CloseSession(); });
        }

        Runner.CloseSession();
    }
}