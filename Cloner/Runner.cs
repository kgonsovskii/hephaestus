using System.Diagnostics;
using System.Text;
using model;

namespace Cloner;

public class Runner
{
    public static void Clean()
    {
        try
        {
            File.Delete(LogFile);
        }
        catch (Exception e)
        {
        }
    }
    public static string LogFile;
    public static string Server;
    public static int LocalSession;
    public static string CurrentTag;
    public static void Kill(string namePart)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            throw new ArgumentException("Name part cannot be null or empty.", nameof(namePart));

        var processes = Process.GetProcesses()
            .Where(p => p.ProcessName.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);

        foreach (var process in processes)
        {
            try
            {
                Console.WriteLine($"Killing: {process.ProcessName} (PID {process.Id})");
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to kill {process.ProcessName} (PID {process.Id}): {ex.Message}");
            }
        }
        Thread.Sleep(1000);
    }

    public static string RdpPassword => System.IO.File.ReadAllText("C:\\Windows\\info.txt").Trim();

    public static void RunIn(string runExe, bool isWait = true, bool isTag = false, int timeout = 0,
        params (string Name, object Value)[] parameters)
    {
        Run(ServerModelLoader.PsExec, true, false, timeout, 
            new ValueTuple<string, object>("-i", LocalSession),
            new ValueTuple<string, object>("-p", RdpPassword),
            new ValueTuple<string, object>("-u", "rdp"),
            new ValueTuple<string, object>(runExe, ArgString(runExe, isWait, isTag, timeout, parameters)));
    }

    public static string ArgString(string runExe, bool isWait = true, bool isTag = false, int timeout=0, params (string Name, object Value)[] parameters)
    {
        char ravno = ' ';
        if (runExe.Contains("SharpRdp"))
            ravno = '=';
        var tag = Environment.TickCount.ToString();
        var prms = parameters.ToList();
        if (isTag)
        {
            prms.Add(new ValueTuple<string, object>("--tag", tag));
            CurrentTag = tag;
        }

        if (isWait)
            prms.Add(new ValueTuple<string, object>("--timeout", timeout));
        return string.Join(' ', prms.Select(p => $"{p.Item1}{ravno}{p.Item2}"));
    }
    
    public static void Run(string runExe, bool isWait = true, bool isTag = false, int timeout=0,  params (string Name, object Value)[] parameters)
    {
        var args = ArgString(runExe, isWait, isTag, timeout, parameters);
        using (Process process = new Process())
        {
            process.StartInfo.FileName = runExe;
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = ServerModelLoader.RootDirStatic;
            
            Console.WriteLine("RUN: " + runExe + " " + args);

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
                    Log(e.Data);
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
                    output.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
                    Log(e.Data);
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

            if (isWait)
            {
                process.WaitForExit();
            }

            if (isTag)
                WaitForLocalTag(CurrentTag, timeout);
            Thread.Sleep(100);
        }
    }

    public static string LocalTag = "C:\\install\\tag_local.txt";
    public static bool WaitForLocalTag(string localTag, int timeout)
    {
        timeout *= 1000;
        var start = Environment.TickCount;
        while (true)
        {
            if (timeout != 0 && Environment.TickCount - start > timeout)
            {
                Log($"Waiting for local tag {localTag} timeout");
                return false;
            }
            string tag = "";
            try
            {
                tag = File.ReadAllText(LocalTag);
            }
            catch (Exception e)
            {
            }

            if (tag.Contains(localTag))
            {
                return true;
            }
            System.Threading.Thread.Sleep(1000);
        }

        return false;
    }
    
    public static void RunPs(string command,bool isWait = true, bool isTag = false, int timeout=0 )
    {
        Run("powershell", isWait, isTag, timeout,
            new ValueTuple<string, object>("-NoProfile", ""),
            new ValueTuple<string, object>("-ExecutionPolicy", "Bypass"),
            new ValueTuple<string, object>("-Command", $"\"{command}\"")
        );
    }
    
    public static void RunPsFile(string psFile, bool isWait = true, bool isTag = false, int timeout=0, params (string Name, object Value)[] parameters)
    {
        var args = parameters.ToList();
        args.Add(new ValueTuple<string, object>("-serverName", Server));
        var argsStr = string.Join(" ", args.Select(p => $"{p.Item1} {p.Item2}"));
        psFile = SysScript(psFile);
        Run("powershell", isWait, isTag, timeout, 
            new ValueTuple<string, object>("-ExecutionPolicy", "Bypass"),
            new ValueTuple<string, object>("-File", $"\"{psFile}\""),
            new ValueTuple<string, object>("-ArgumentList", $"{argsStr}")
        );
    }

    public static string SysScript(string scriptName)
    {
        return Path.Combine(ServerModelLoader.SysDirStatic, scriptName + ".ps1");
    }
    
    public static void Log(string s)
    {
        Console.WriteLine(s);
    }
}