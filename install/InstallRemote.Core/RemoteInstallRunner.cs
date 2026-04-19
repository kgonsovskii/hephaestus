using System.Diagnostics;
using System.Text;

namespace InstallRemote;

/// <summary>Cross-platform SSH remote install (sshpass + remote bash script). Windows sshpass discovery: <see cref="SshPassBootstrap"/>.</summary>
public static class RemoteInstallRunner
{
    public const string DefaultRemoteScriptFileName = "install-remote.txt";

    private static readonly string[] SshCommonOpts =
    [
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "ConnectTimeout=30",
        "-o", "ServerAliveInterval=15",
        "-o", "ServerAliveCountMax=4"
    ];

    public static string LoadRemoteScriptFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Remote install script not found.", path);

        var text = File.ReadAllText(path, Encoding.UTF8);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal).TrimEnd() + "\n";
    }

    public static string? FindSshPassOnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "sshpass.exe", "sshpass" })
            {
                var full = Path.Combine(dir.Trim(), name);
                if (File.Exists(full))
                    return full;
            }
        }

        return null;
    }

    public static void TryRemoveKnownHostsEntryForHost(string host, Action<string>? logInfo)
    {
        host = host.Trim();
        if (host.Length == 0)
            return;

        var keygen = FindSshKeygen();
        if (keygen == null)
        {
            logInfo?.Invoke(
                "InstallRemote: ssh-keygen not found; if SSH fails with host key changed, run: ssh-keygen -R \"<host>\"");
            return;
        }

        logInfo?.Invoke($"InstallRemote: Clearing known_hosts for {host} (ssh-keygen -R) …");
        var psi = new ProcessStartInfo
        {
            FileName = keygen,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-R");
        psi.ArgumentList.Add(host);

        try
        {
            using var p = Process.Start(psi);
            if (p == null)
                return;
            p.WaitForExit();
        }
        catch (Exception ex)
        {
            logInfo?.Invoke($"InstallRemote: ssh-keygen -R skipped ({ex.Message})");
        }
    }

    public static string? FindSshKeygen()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(path))
        {
            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var name in new[] { "ssh-keygen.exe", "ssh-keygen" })
                {
                    var full = Path.Combine(dir.Trim(), name);
                    if (File.Exists(full))
                        return full;
                }
            }
        }

        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var openSsh = Path.Combine(system, "OpenSSH", "ssh-keygen.exe");
        if (File.Exists(openSsh))
            return openSsh;

        var windir = Environment.GetEnvironmentVariable("WINDIR");
        if (!string.IsNullOrEmpty(windir))
        {
            var sysnative = Path.Combine(windir, "Sysnative", "OpenSSH", "ssh-keygen.exe");
            if (File.Exists(sysnative))
                return sysnative;
        }

        return null;
    }

    /// <param name="emitLineAsync">Each stdout/stderr line (stderr prefixed with <c>[stderr] </c>).</param>
    /// <param name="onProcessStarted">Invoked after <see cref="Process.Start"/> so callers can track PID / kill.</param>
    public static async Task<int> RunRemoteInstallAsync(
        string sshPassExecutable,
        string host,
        string user,
        string password,
        string remoteScriptText,
        Func<string, CancellationToken, Task> emitLineAsync,
        Action<Process>? onProcessStarted,
        CancellationToken cancellationToken = default)
    {
        TryRemoveKnownHostsEntryForHost(host, msg =>
        {
            try
            {
                emitLineAsync(msg, cancellationToken).GetAwaiter().GetResult();
            }
            catch
            {
                // best-effort log line
            }
        });

        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(remoteScriptText));
        var remoteShell = $"echo {b64} | base64 -d | bash";

        var sshArgs = new List<string> { "-e", "ssh", "-tt" };
        sshArgs.AddRange(SshCommonOpts);
        sshArgs.Add($"{user}@{host}");
        sshArgs.Add(remoteShell);

        var psi = new ProcessStartInfo
        {
            FileName = sshPassExecutable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in sshArgs)
            psi.ArgumentList.Add(a);

        psi.Environment["SSHPASS"] = password;

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start sshpass/ssh process.");

        onProcessStarted?.Invoke(proc);

        var stdout = PumpLinesAsync(proc.StandardOutput, prefix: null, emitLineAsync, cancellationToken);
        var stderr = PumpLinesAsync(proc.StandardError, "[stderr] ", emitLineAsync, cancellationToken);
        try
        {
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!proc.HasExited)
        {
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }

            throw;
        }

        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        return proc.ExitCode;
    }

    private static async Task PumpLinesAsync(
        StreamReader reader,
        string? prefix,
        Func<string, CancellationToken, Task> emit,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                    break;
                var msg = prefix == null ? line : prefix + line;
                await emit(msg, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
