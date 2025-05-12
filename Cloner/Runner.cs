using System.Diagnostics;
using System.Text;
using model;

namespace Cloner;

public class Runner
{
    public const int LocalTagTimeOut = 70;
    public const int CommandTimeOut = LocalTagTimeOut;
    public const int RebootTimeOut = 110;
    public const int StageTimeOut = 1800;

    public static void Reboot()
    {
        Runner.RunPsFile("install-reboot", true, false, RebootTimeOut);
    }
    
    public static void Clean()
    {
        try
        {
            File.Delete(LogFile);
        }
        catch (Exception e)
        {
        }

        LastLog = DateTime.Now;
    }
    public static string LogFile;
    public static string Server;
    public static string RunInTag;
    public static bool RunInWithRestart;
    private static void Kill(string namePart)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            throw new ArgumentException("Name part cannot be null or empty.", nameof(namePart));

        var processes = Process.GetProcesses()
            .Where(p => p.ProcessName.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

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
        Thread.Sleep(500);
    }

    public static void RunIn(string runExe, bool isWait = true, bool isTag = false, bool remoteTag = false, int timeout = 0,
        params (string Name, object Value)[] parameters)
    {
        try
        {
            RunInTag = Environment.TickCount.ToString();
            Run(runExe, isWait, isTag, timeout, parameters);
        }
        catch (Exception e)
        {
            Log("Run In Error, restarting...");
            Thread.Sleep(1000);
            RunInTag = "";
            if (RunInWithRestart)
            {
                Runner.RunPsFile("install-reboot", true, false, 90);
                Thread.Sleep(1000);
            }

            RunIn(runExe, isWait, isTag, remoteTag, timeout, parameters);
        }

    }

    public static string ArgString(string runExe, bool isWait = true, bool isTag = false, int timeout=0, params (string Name, object Value)[] parameters)
    {
        char ravno = ' ';
        if (runExe.Contains("SharpRdp"))
            ravno = '=';

        var prms = parameters.ToList();
        if (isTag && !string.IsNullOrEmpty(RunInTag) && !runExe.Contains("powershell"))
        {
            prms.Add(new ValueTuple<string, object>("--tag", RunInTag)); ;
        }

        if (isWait && !runExe.Contains("powershell"))
            prms.Add(new ValueTuple<string, object>("--timeout", timeout));
        var result = string.Join(' ', prms.Select(p => $"{p.Item1}{ravno}{p.Item2}"));
        result = result.Replace("$tag", RunInTag);
        return result;
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
            
            Log("RUN: " + runExe + " " + args);

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
                    Log(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log(e.Data);
                    output.AppendLine(DateTime.Now.ToString() + ": " + e.Data);
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
            {
                WaitForLocalTag(RunInTag, LocalTagTimeOut);
            }

            Thread.Sleep(300);
        }
    }

    public static string LocalTag = "C:\\install\\tag_local.txt";
    public static bool WaitForLocalTag(string localTag, int timeout)
    {
        Log("WaitForLocalTag:" + localTag);
        timeout *= 1000;
        var start = Environment.TickCount;
        while (true)
        {
            if (timeout != 0 && Environment.TickCount - start > timeout)
            {
                Log($"Waiting for local tag {localTag} timeout");
                throw new InvalidOperationException($"Waiting for local tag {localTag} timeout");
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
                var res = tag.Contains("ok");
                if (!res)
                    throw new InvalidOperationException($"Waiting for local tag {localTag} error");
                return res;
            }
            System.Threading.Thread.Sleep(1000);
        }

        return false;
    }
    
    public static void WaitForRemoteTag(string remoteTag, int timeout)
    {
        Log("WaitForRemoteTag:" + remoteTag);
        RunPsFile("install-wait", true, true, timeout,
            new ValueTuple<string, object>("-timeout", timeout),
            new ValueTuple<string, object>("-tag", RunInTag));
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

    public static DateTime LastLog;
 
 
    public static void Log(string s)
    {
        s = DateTime.Now.ToString() + "[ " + (DateTime.Now - LastLog).TotalSeconds + "sec] "  + s;
        LastLog = DateTime.Now;
        Console.WriteLine(s);
        try
        {
            File.AppendAllText(LogFile, s + Environment.NewLine);
        }
        catch (Exception exception)
        {
        }
    }

    public static void OpenSession()
    {
        CloseSession();
    }

    public static void CloseSession()
    {
        Runner.Kill("SharpRdp");
        Runner.Kill("powershell");
        Runner.Kill("PsExec64");
    }
}