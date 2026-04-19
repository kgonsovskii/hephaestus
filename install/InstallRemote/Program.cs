using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;
using System.Text;

internal static class Program
{
    private const string CredsFileName = "install-remote-creds.txt";

    /// <summary>GitHub release tag for <c>sharpninja/sshpass-win64</c> portable zip (Windows only); not an SSH secret.</summary>
    private const string SshPassWin64ReleaseTag = "1.10.0";

    private static readonly string[] SshCommonOpts =
    [
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "ConnectTimeout=30",
        "-o", "ServerAliveInterval=15",
        "-o", "ServerAliveCountMax=4"
    ];

    public static async Task<int> Main(string[] args)
    {
        var creds = LoadCredsFromFileOrThrow();
        var server = args.Length > 0 ? args[0].Trim() : creds.Server;
        var login = args.Length > 1 ? args[1].Trim() : creds.Login;
        var password = args.Length > 2 ? args[2] : creds.Password;

        try
        {
            TryRemoveKnownHostsEntryForHost(server);

            var sshpass = await EnsureSshPassAsync();
            Environment.SetEnvironmentVariable("SSHPASS", password);

            Console.WriteLine($"Remote install -> {login}@{server}");
            Console.WriteLine("[1/1] SSH: install git, clone repo to $HOME/hephaestus (remote user), run install.sh");

            var remoteCmd = LoadRemoteScriptText();

            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(remoteCmd));
            var remoteShell = $"echo {b64} | base64 -d | bash";

            var sshArgs = new List<string> { "-e", "ssh", "-tt" };
            sshArgs.AddRange(SshCommonOpts);
            sshArgs.Add($"{login}@{server}");
            sshArgs.Add(remoteShell);

            var code = await RunSshPassStreamingAsync(sshpass, sshArgs);
            if (code != 0)
            {
                Console.Error.WriteLine($"Remote install failed with exit {code}");
                return code;
            }

            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

        private static void TryRemoveKnownHostsEntryForHost(string host)
    {
        host = host.Trim();
        if (host.Length == 0)
            return;

        var keygen = FindSshKeygen();
        if (keygen == null)
        {
            Console.WriteLine(
                "InstallRemote: ssh-keygen not found; if SSH fails with host key changed, run: ssh-keygen -R \"<host>\"");
            return;
        }

        Console.WriteLine($"InstallRemote: Clearing known_hosts for {host} (ssh-keygen -R) …");
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
            Console.WriteLine($"InstallRemote: ssh-keygen -R skipped ({ex.Message})");
        }
    }

    private static string? FindSshKeygen()
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

    private sealed record RemoteCreds(string Server, string Login, string Password);

    private static RemoteCreds LoadCredsFromFileOrThrow()
    {
        var path = ResolveCredsPath();
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Expected {CredsFileName} with three lines (host, login, password). " +
                $"Create or edit {CredsFileName} next to the app or under install/. Looked for: {path}",
                path);

        var lines = File.ReadAllText(path, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        var taken = new List<string>();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length == 0)
                continue;
            if (t.StartsWith("#", StringComparison.Ordinal))
                continue;
            taken.Add(t);
            if (taken.Count == 3)
                break;
        }

        if (taken.Count < 3)
            throw new InvalidOperationException(
                $"{path} must contain three non-empty, non-comment lines: SSH host, login, password (got {taken.Count}).");

        return new RemoteCreds(taken[0], taken[1], taken[2]);
    }

    private static string ResolveCredsPath()
    {
        var besideExe = Path.Combine(AppContext.BaseDirectory, CredsFileName);
        if (File.Exists(besideExe))
            return besideExe;

        var fromRepoInstall = Path.Combine(Environment.CurrentDirectory, "install", CredsFileName);
        if (File.Exists(fromRepoInstall))
            return fromRepoInstall;

        return besideExe;
    }

        private static string LoadRemoteScriptText()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "install-remote.txt");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "install-remote.txt not found next to the executable. Rebuild so install-remote.txt is copied from install/.",
                path);

        var text = File.ReadAllText(path, Encoding.UTF8);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal).TrimEnd() + "\n";
    }

        private static async Task<int> RunSshPassStreamingAsync(string fileName, List<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi };
        p.Start();

        var outStream = Console.OpenStandardOutput();
        var errStream = Console.OpenStandardError();
        await Task.WhenAll(
            p.StandardOutput.BaseStream.CopyToAsync(outStream),
            p.StandardError.BaseStream.CopyToAsync(errStream));
        await outStream.FlushAsync();
        await errStream.FlushAsync();

        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    private static async Task<string> EnsureSshPassAsync()
    {
        var found = FindSshPassOnPath();
        if (found != null)
            return found;

        found = FindSshPassUnderChocolateyLib();
        if (found != null)
            return found;

        found = FindSshPassUnderLocalTools();
        if (found != null)
            return found;

        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "choco.exe"))
            && IsAdministrator()
            && await TryChocolateyInstallSshPassAsync())
        {
            found = FindSshPassOnPath() ?? FindSshPassUnderChocolateyLib();
            if (found != null)
            {
                Console.WriteLine($"sshpass (Chocolatey): {found}");
                return found;
            }
        }

        found = await DownloadPortableSshPassAsync();
        Console.WriteLine($"sshpass ready: {found}");
        return found;
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryChocolateyInstallSshPassAsync()
    {
        var choco = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "choco.exe");
        if (!File.Exists(choco))
            return false;

        Console.WriteLine("Trying Chocolatey package sshpass-win64 (optional)...");
        var psi = new ProcessStartInfo
        {
            FileName = choco,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in new[] { "install", "sshpass-win64", "-y", "--no-progress", "--limit-output" })
            psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi };
        p.Start();
        await p.WaitForExitAsync();
        return p.ExitCode == 0 || p.ExitCode == 3010;
    }

    private static string? FindSshPassOnPath()
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

    private static string? FindSshPassUnderChocolateyLib()
    {
        var lib = Environment.GetEnvironmentVariable("ChocolateyInstall");
        lib = string.IsNullOrEmpty(lib)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "lib")
            : Path.Combine(lib, "lib");

        if (!Directory.Exists(lib))
            return null;
        try
        {
            var hit = Directory.EnumerateFiles(lib, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
            return hit;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindSshPassUnderLocalTools()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hephaestus-tools");
        if (!Directory.Exists(root))
            return null;
        try
        {
            return Directory.EnumerateFiles(root, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> DownloadPortableSshPassAsync()
    {
        var tag = SshPassWin64ReleaseTag;
        var destRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "hephaestus-tools",
            $"sshpass-win64-{tag}");
        Directory.CreateDirectory(destRoot);

        var existing = Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (existing != null)
            return existing;

        var zipUrl = $"https://github.com/sharpninja/sshpass-win64/releases/download/v{tag}/sshpass-win64-{tag}.zip";
        var tmpZip = Path.Combine(Path.GetTempPath(), $"hephaestus-sshpass-{tag}.zip");

        Console.WriteLine($"Downloading portable sshpass-win64 v{tag} from GitHub...");
        using (var http = new HttpClient())
        {
            await using var input = await http.GetStreamAsync(zipUrl);
            await using var output = File.Create(tmpZip);
            await input.CopyToAsync(output);
        }

        try
        {
            ZipFile.ExtractToDirectory(tmpZip, destRoot, overwriteFiles: true);
        }
        finally
        {
            try { File.Delete(tmpZip); } catch {  }
        }

        var exe = Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"sshpass.exe not found after extracting. URL: {zipUrl}");
        return exe;
    }
}
