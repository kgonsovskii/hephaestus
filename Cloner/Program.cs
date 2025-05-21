using model;

namespace Cloner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        string direct = "false";
        var server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0].Trim();
        }

        var login = "";
        var password = "";
        var logFile = "";
        var serverIp = "";
        
        if (args.Length >= 3)
        {
            login = args[1].Trim();
            password = args[2].Trim();
            logFile = "_log.txt";
            serverIp = server;
            direct = "true";
        }
        else
        {
            var model = ServerModelLoader.LoadServer(server);
            logFile = model.UserCloneLog;
            server = model.Server;
            serverIp = model.CloneModel.CloneServerIp;
            login = model.CloneModel.CloneUser;
            password = model.CloneModel.ClonePassword;
            Console.WriteLine($"Cloning model server {server}");
        }
        
        Killer.StartKilling(true, string.IsNullOrEmpty(direct));
        
        Runner.Log("Starting cloner:" + server);
        if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
        {
            Runner.Log("Cloning direct:" + server + ":" + login + ":" + password);
        }



        Runner.LogFile = logFile;
        Runner.Server = server;
        Runner.User = login;
        Runner.Password = password;
        Runner.Direct = direct.ToString();
        Runner.Log("WinUser=" + Environment.UserName + ", LogFile: " + Runner.LogFile);
        Runner.Clean();
        
        Runner.RunPsFile("install-x");
        if (NetworkUtils.Test(serverIp))
        {
            Runner.Log($"No preparation for CloneServerIp {serverIp}");
        }
        else
        {
            Runner.Log($"Do preparation for CloneServerIp {serverIp}");
            Prepare(serverIp, login, password, logFile);
        }
        
        Runner.Reboot();
        
        Runner.RunPsFile("install-copy");

        Install(serverIp, login, password, logFile);

        Runner.Direct = "true";
        Runner.Server = serverIp;
        Runner.RunPsFile("publish", true, false, 0);
        Runner.Reboot();

        Runner.CloseSession();
        Killer.StopKilling();

        Runner.Log("ACCOMPLISHED.");
    }

    public static void Prepare(string serverIp, string user, string password, string logFile)
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
                new ValueTuple<string, object>("--server", serverIp),
                new ValueTuple<string, object>("--username", user),
                new ValueTuple<string, object>("--password", password),
                new ValueTuple<string, object>("--logfile", $"\"{logFile}\""),
                new ValueTuple<string, object>("--command", $"\"{cmd}\""));
            Runner.RunInTag = "";
        }

        Runner.CloseSession();
    }

    public static void Install(string serverIp, string user, string password, string logFile)
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
                new ValueTuple<string, object>("--server", serverIp),
                new ValueTuple<string, object>("--username", user),
                new ValueTuple<string, object>("--password", password),
                new ValueTuple<string, object>("--logfile", $"\"{logFile}\""),
                new ValueTuple<string, object>("--command", $"\"{cmd}\""));
            
            Runner.WaitForRemoteTag(Runner.RunInTag, Runner.StageTimeOut);
            Runner.RunInTag = "";
        }

        Runner.CloseSession();
    }
}