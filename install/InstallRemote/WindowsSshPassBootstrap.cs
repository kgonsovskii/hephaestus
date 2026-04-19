using System.Diagnostics;
using System.IO.Compression;
using System.Security.Principal;
using System.Text;
using InstallRemote;

namespace InstallRemoteTool;

/// <summary>Windows-only discovery / download of sshpass for the install-remote executable.</summary>
internal static class WindowsSshPassBootstrap
{
    private const string SshPassWin64ReleaseTag = "1.10.0";

    public static async Task<string> EnsureAsync()
    {
        var found = RemoteInstallRunner.FindSshPassOnPath();
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
            && await TryChocolateyInstallSshPassAsync().ConfigureAwait(false))
        {
            found = RemoteInstallRunner.FindSshPassOnPath() ?? FindSshPassUnderChocolateyLib();
            if (found != null)
            {
                Console.WriteLine($"sshpass (Chocolatey): {found}");
                return found;
            }
        }

        found = await DownloadPortableSshPassAsync().ConfigureAwait(false);
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
        await p.WaitForExitAsync().ConfigureAwait(false);
        return p.ExitCode == 0 || p.ExitCode == 3010;
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
            return Directory.EnumerateFiles(lib, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
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
            await using var input = await http.GetStreamAsync(new Uri(zipUrl)).ConfigureAwait(false);
            await using var output = File.Create(tmpZip);
            await input.CopyToAsync(output).ConfigureAwait(false);
        }

        try
        {
            ZipFile.ExtractToDirectory(tmpZip, destRoot, overwriteFiles: true);
        }
        finally
        {
            try
            {
                File.Delete(tmpZip);
            }
            catch
            {
                // ignored
            }
        }

        return Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
               ?? throw new InvalidOperationException($"sshpass.exe not found after extracting. URL: {zipUrl}");
    }
}
