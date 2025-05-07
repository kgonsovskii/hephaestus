using System.Diagnostics;

namespace model;

public class Killer
{
    private static Task foregroundThread;
    private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public static void StartKilling(bool all)
    {
        KillThem(all, true);
        foregroundThread = new Task(() =>
        {
            while (true)
            {
                try
                {
                    KillThem(all, false);
                }
                catch (Exception)
                {
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    break;
                Thread.Sleep(3000);
            }
        }, cancellationTokenSource.Token);
        
        foregroundThread.Start();

        Console.WriteLine("Press Enter to exit the main thread.");
    }

    public static void StopKilling()
    {
        cancellationTokenSource.Cancel();
    }

    private static void KillThem(bool all, bool ps)
    {
        var name = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
        Kill(name);
        if (all)
        {
            Kill("refiner");
            Kill("cloner");
            Kill("packer");
            Kill("desktop.bat");
            Kill("SharpRdp");
            if (ps)
                Kill("powershell");
        }
        Thread.Sleep(500);
    }
    
    private static void Kill(string pattern)
    {
        try
        {
            int currentProcessId = Process.GetCurrentProcess().Id;

            var processesToKill = Process.GetProcesses()
                .Where(p => p.Id != currentProcessId && 
                            p.ProcessName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);


            foreach (var process in processesToKill)
            {
                try
                {
                    Console.WriteLine($"Killing process {process.ProcessName} (ID: {process.Id})...");
                    process.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not kill process {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}