using System.Diagnostics;
using System.Text;
using System.Text.Json;
using model;

namespace Commons;

public class ServerResult
{
    public ServerModel? ServerModel;

    public Exception? Exception;
}

public partial class ServerService
{
    private readonly ServerModelLoader _loader;

    public ServerService(ServerModelLoader loader) => _loader = loader;

    public ServerModelLoader Loader => _loader;

    public IPanelServerPaths Paths => _loader.Paths;

    public ServerLayoutPaths Layout() => new(Paths);

    public JsonSerializerOptions Jso => _loader.Jso;

    private void ServerCommons(ServerModel serverModel)
    {
        UpdateTabs(serverModel);

        UpdatePacks(serverModel);

        UpdateBux(serverModel);

        UpdateDnSponsor(serverModel);
    }

    public string ServerDir => Paths.UserDataDir;

    public string EmbeddingsDir => Path.Combine(ServerDir, "embeddings");

    public string FrontDir => Path.Combine(ServerDir, "front");

    public string DataFile => Paths.DataFile;

    public string GetIconPath() => Path.Combine(ServerDir, "troyan.ico");

    public string GetExePath() => Path.Combine(ServerDir, "troyan.exe");

    public string GetEmbeddingPath(string embeddingName) => Path.Combine(EmbeddingsDir, embeddingName);

    public void DeleteEmbedding(string embeddingName) => File.Delete(GetEmbeddingPath(embeddingName));

    public string GetFrontPath(string embeddingName) => Path.Combine(FrontDir, embeddingName);

    public void DeleteFront(string embeddingName) => File.Delete(GetFrontPath(embeddingName));

    public string UserPackLogPath => Path.Combine(Paths.UserDataDir, "pack.log");

    public string UserPostLogPath => Path.Combine(Paths.UserDataDir, "post.log");

    public string UserCloneLogPath => Path.Combine(Paths.UserDataDir, "clone.log");

    public ServerModel GetServerLite()
    {
        var server = _loader.Load();
        server.PanelHomeDirectory = Paths.UserDataDir;
        return server;
    }

    public void SaveServerLite(ServerModel server)
    {
        server.PanelHomeDirectory = Paths.UserDataDir;
        _loader.Save(server);
    }

    public ServerResult GetServerHard()
    {
        var server = GetServerLite();
        try
        {
            ServerCommons(server);

            server.Embeddings = new List<string>();
            if (Directory.Exists(EmbeddingsDir))
                server.Embeddings = Directory.GetFiles(EmbeddingsDir).Select(a => Path.GetFileName(a)).ToList();

            server.Front = new List<string>();
            if (Directory.Exists(FrontDir))
                server.Front = Directory.GetFiles(FrontDir).Select(a => Path.GetFileName(a)).ToList();

            SaveServerLite(server);

            return new ServerResult() { ServerModel = server };
        }
        catch (Exception e)
        {
            server.PostModel.LastResult = e.Message;
            return new ServerResult() { Exception = e, ServerModel = server };
        }
    }

    public string Reboot()
    {
        if (!Directory.Exists(Paths.UserDataDir))
            return "";
        try
        {
            return RunScript(PanelServerIdentity.DefaultKey, "reboot", "nolog", null,
                new ValueTuple<string, object>("serverName", PanelServerIdentity.DefaultKey));
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    public string RunExe(string exe, string? arguments = null)
    {
        var args = PanelServerIdentity.DefaultKey;
        if (!string.IsNullOrEmpty(arguments))
            args += $" {arguments}";
        var sa = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            CreateNoWindow = false,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        var process = new Process { StartInfo = sa };
        process.Start();
        return "OK";
    }

    public string SysScript1(string scriptName) => Path.Combine(_loader.Paths.SysDir, scriptName + ".ps1");

    public string RunScript(string server, string scriptFile, string LogFile, Action<string>? logger,
        params (string Name, object Value)[] parameters)
    {
        try
        {
            File.Delete(LogFile);
        }
        catch (Exception)
        {
        }

        scriptFile = SysScript1(scriptFile);
        using (var process = new Process())
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -file \"{scriptFile}\" " +
                                          string.Join(" ", parameters.Select(p => $"-{p.Name} {p.Value}"));
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Paths.UserDataDir;

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(DateTime.Now + ": " + e.Data);
                    logger?.Invoke(e.Data);
                    try
                    {
                        File.AppendAllText(LogFile, DateTime.Now + ": " + e.Data + Environment.NewLine);
                    }
                    catch (Exception)
                    {
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(DateTime.Now + ": " + e.Data);
                    logger?.Invoke(e.Data);
                    try
                    {
                        File.AppendAllText(LogFile, DateTime.Now + ": " + e.Data + Environment.NewLine);
                    }
                    catch (Exception)
                    {
                    }
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var res = error + "\r\n" + output;

            try
            {
                File.AppendAllText(LogFile, DateTime.Now + ": " + "ACK");
            }
            catch (Exception)
            {
            }

            return res + Environment.NewLine + "SYS ACCOMPLISHED";
        }
    }
}
