using model;

namespace RemoteEnabler;

internal static class Program
{
    private static void Main(string[] args)
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
        Runner.OpenSession();
        
        if (NetworkUtils.Test(serverIp))
        {
            Runner.Log($"No preparation for CloneServerIp {serverIp}");
        }
        else
        {
            Runner.Log($"Do preparation for CloneServerIp {serverIp}");
            Prepare(serverIp, login, password, logFile);
        }
        Runner.CloseSession();
        
        Runner.Log("ACCOMPLISHED.");
    }

    private static string SharpRdp
    {
        get
        {
            var dir = Path.GetFullPath(AppContext.BaseDirectory);
            while (true)
            {
                var candidate = Path.Combine(dir, "rdp", "SharpRdp.exe");
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                var parent = Directory.GetParent(dir);
                if (parent == null)
                    throw new FileNotFoundException("SharpRdp.exe not found.", Path.Combine(dir, "rdp", "SharpRdp.exe"));
                dir = parent.FullName;
            }
        }
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
        
        foreach (var c in Commands)
        {
            var cmd = c;
            Runner.RunIn(SharpRdp, true, true, false, Runner.CommandTimeOut,
                new ValueTuple<string, object>("--server", serverIp),
                new ValueTuple<string, object>("--username", user),
                new ValueTuple<string, object>("--password", password),
                new ValueTuple<string, object>("--logfile", $"\"{logFile}\""),
                new ValueTuple<string, object>("--command", $"\"{cmd}\""));
            Runner.RunInTag = "";
        }

        Runner.CloseSession();
    }
}