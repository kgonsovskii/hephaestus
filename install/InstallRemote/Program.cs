using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;
using System.Text;

internal static class Program
{
    private const string DefaultServer = "216.203.21.239";
    private const string DefaultLogin = "root";
    private const string DefaultPassword = "1!Ogviobhuetly";
    private const string SshPassVersion = "1.10.0";

    private static readonly string[] SshCommonOpts =
    [
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "ConnectTimeout=30",
        "-o", "ServerAliveInterval=15",
        "-o", "ServerAliveCountMax=4"
    ];

    public static async Task<int> Main(string[] args)
    {
        var server = args.Length > 0 ? args[0] : DefaultServer;
        var login = args.Length > 1 ? args[1] : DefaultLogin;
        var password = args.Length > 2 ? args[2] : DefaultPassword;

        try
        {
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

    /// <summary>Loads <c>install/install-remote.txt</c> (copied next to the exe). Single source with PS1 and install-remote.sh.</summary>
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

    /// <summary>
    /// OpenSSH on Windows often block-buffers when attached to an inherited console (especially from IDE hosts).
    /// Redirect pipes and stream-copy to our console while the process runs so output appears incrementally.
    /// </summary>
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
        var destRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "hephaestus-tools",
            $"sshpass-win64-{SshPassVersion}");
        Directory.CreateDirectory(destRoot);

        var existing = Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (existing != null)
            return existing;

        var zipUrl = $"https://github.com/sharpninja/sshpass-win64/releases/download/v{SshPassVersion}/sshpass-win64-{SshPassVersion}.zip";
        var tmpZip = Path.Combine(Path.GetTempPath(), $"hephaestus-sshpass-{SshPassVersion}.zip");

        Console.WriteLine($"Downloading portable sshpass-win64 v{SshPassVersion} from GitHub...");
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
            try { File.Delete(tmpZip); } catch { /* ignore */ }
        }

        var exe = Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"sshpass.exe not found after extracting. URL: {zipUrl}");
        return exe;
    }
}
