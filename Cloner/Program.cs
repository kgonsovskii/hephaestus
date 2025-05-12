using model;

namespace Cloner;

internal static class Program
{
    private static async Task Main(string[] args)
    {

        var server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }
        
        Runner.Log("Starting cloner:" + server);
        Killer.StartKilling(true);


        var model = ServerModelLoader.LoadServer(server);
        Console.WriteLine($"Cloning server {server}");

        Runner.LogFile = model.UserCloneLog;
        Runner.Server = model.Server;
        Runner.Log("WinUser=" + Environment.UserName + ", LogFile: " + Runner.LogFile);
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
        
        Runner.Reboot();
        
        Runner.RunPsFile("install-copy");

        Install(model);

        Runner.RunPsFile("publish", true, false, 0,
            new ValueTuple<string, object>("-serverIp", model.CloneModel.CloneServerIp),
            new ValueTuple<string, object>("-user", model.CloneModel.CloneUser),
            new ValueTuple<string, object>("-password", model.CloneModel.ClonePassword),
            new ValueTuple<string, object>("-direct", "true")
        );

        Runner.Reboot();

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
        Runner.OpenSession();
        
        Runner.RunPsFile("install-pre", true, false, 0, new ValueTuple<string, object>("-reboot", "true"));

        foreach (var c in Commands)
        {
            var cmd = c;
            Runner.RunIn(ServerModelLoader.SharpRdp, true, true, false, Runner.CommandTimeOut,
                new ValueTuple<string, object>("--server", model.CloneModel.CloneServerIp),
                new ValueTuple<string, object>("--username", model.CloneModel.CloneUser),
                new ValueTuple<string, object>("--password", model.CloneModel.ClonePassword),
                new ValueTuple<string, object>("--logfile", $"\"{model.UserCloneLog}\""),
                new ValueTuple<string, object>("--command", $"\"{cmd}\""));
            Runner.RunInTag = "";
        }

        Runner.CloseSession();
    }

    public static void Install(ServerModel model)
    {
        string[] files = new[]
        {
            "install-0",
            "install-sql",
            "install-sql2",
            "install-web",
            "install-web2",
            "install-trigger"
        };
        
        Runner.OpenSession();
        foreach (var file in files)
        {
            Runner.Reboot();
            
            var cmd = $". 'C:\\Install\\{file}.ps1'; Set-Content -Path 'C:\\Install\\tag.txt' -Value '$tag'";

            Runner.RunInWithRestart = true;
            Runner.RunIn(ServerModelLoader.SharpRdp, true, true, true, Runner.StageTimeOut,
                new ValueTuple<string, object>("--server", model.CloneModel.CloneServerIp),
                new ValueTuple<string, object>("--username", model.CloneModel.CloneUser),
                new ValueTuple<string, object>("--password", model.CloneModel.ClonePassword),
                new ValueTuple<string, object>("--logfile", $"\"{model.UserCloneLog}\""),
                new ValueTuple<string, object>("--command", $"\"{cmd}\""));
            
            Runner.WaitForRemoteTag(Runner.RunInTag, Runner.StageTimeOut);
            Runner.RunInTag = "";
        }

        Runner.CloseSession();
    }
}