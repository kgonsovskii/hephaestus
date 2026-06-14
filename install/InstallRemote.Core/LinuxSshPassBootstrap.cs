using System.Diagnostics;
using System.Runtime.Versioning;

namespace InstallRemote;

/// <summary>Linux: resolve <c>sshpass</c> on PATH or install via apt when missing.</summary>
[SupportedOSPlatform("linux")]
internal static class LinuxSshPassBootstrap
{
    private static readonly string[] KnownPaths = ["/usr/bin/sshpass", "/bin/sshpass"];

    public static async Task<string> EnsureAsync(Action<string>? logInfo, CancellationToken cancellationToken)
    {
        var found = RemoteInstallRunner.FindSshPassOnPath() ?? FindKnownPath();
        if (found != null)
            return found;

        logInfo?.Invoke("sshpass not on PATH; installing via apt…");

        if (!await TryAptInstallAsync(logInfo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "sshpass not found on PATH and automatic install via apt failed (run: sudo apt install sshpass).");
        }

        found = RemoteInstallRunner.FindSshPassOnPath() ?? FindKnownPath();
        if (found != null)
        {
            logInfo?.Invoke($"sshpass ready: {found}");
            return found;
        }

        throw new InvalidOperationException("sshpass install completed but binary not found on PATH.");
    }

    private static string? FindKnownPath()
    {
        foreach (var path in KnownPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static async Task<bool> TryAptInstallAsync(Action<string>? logInfo, CancellationToken cancellationToken)
    {
        if (await IsRootAsync(cancellationToken).ConfigureAwait(false))
            return await RunAptInstallAsync(fileName: ResolveAptGet(), useSudo: false, logInfo, cancellationToken)
                .ConfigureAwait(false);

        if (File.Exists("/usr/bin/sudo"))
        {
            logInfo?.Invoke("sshpass: not root; trying sudo -n apt-get install…");
            return await RunAptInstallAsync(fileName: "/usr/bin/sudo", useSudo: true, logInfo, cancellationToken)
                .ConfigureAwait(false);
        }

        logInfo?.Invoke("sshpass: not root and sudo unavailable.");
        return false;
    }

    private static string ResolveAptGet()
    {
        if (File.Exists("/usr/bin/apt-get"))
            return "/usr/bin/apt-get";
        if (File.Exists("/bin/apt-get"))
            return "/bin/apt-get";
        return "apt-get";
    }

    private static async Task<bool> IsRootAsync(CancellationToken cancellationToken)
    {
        var id = File.Exists("/usr/bin/id") ? "/usr/bin/id" : "id";
        var psi = new ProcessStartInfo
        {
            FileName = id,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-u");

        using var proc = Process.Start(psi);
        if (proc == null)
            return false;

        var output = await proc.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return proc.ExitCode == 0 && output.Trim() == "0";
    }

    private static async Task<bool> RunAptInstallAsync(
        string fileName,
        bool useSudo,
        Action<string>? logInfo,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (useSudo)
        {
            psi.ArgumentList.Add("-n");
            psi.ArgumentList.Add(ResolveAptGet());
        }

        foreach (var a in new[] { "install", "-y", "-o", "DPkg::Lock::Timeout=300", "sshpass" })
            psi.ArgumentList.Add(a);

        psi.Environment["DEBIAN_FRONTEND"] = "noninteractive";

        using var proc = Process.Start(psi);
        if (proc == null)
            return false;

        var stdout = PumpLinesAsync(proc.StandardOutput, null, logInfo, cancellationToken);
        var stderr = PumpLinesAsync(proc.StandardError, "[stderr] ", logInfo, cancellationToken);
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        return proc.ExitCode == 0;
    }

    private static async Task PumpLinesAsync(
        StreamReader reader,
        string? prefix,
        Action<string>? logInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                    break;
                logInfo?.Invoke(prefix == null ? line : prefix + line);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
