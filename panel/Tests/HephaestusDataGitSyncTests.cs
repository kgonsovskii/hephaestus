using System.Diagnostics;
using FluentAssertions;
using Git;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

[TestClass]
public sealed class HephaestusDataGitSyncTests
{
    [TestMethod]
    public void Sync_KeepsLocalServerJson_WhenOriginHasOlderCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "hep-data-git-" + Guid.NewGuid().ToString("N"));
        var origin = Path.Combine(root, "origin.git");
        var clone = Path.Combine(root, "clone");
        Directory.CreateDirectory(root);

        try
        {
            RunGit($"init --bare \"{origin}\"", root);
            RunGit($"clone \"{origin}\" \"{clone}\"", root);
            RunGit("config user.email test@local", clone);
            RunGit("config user.name Test", clone);

            Directory.CreateDirectory(Path.Combine(clone, "gonzik", "server"));
            var serverPath = Path.Combine(clone, "gonzik", "server", "server.json");
            File.WriteAllText(serverPath, "{\"landingFtp\":\"old\"}\n");
            RunGit("add -A", clone);
            RunGit("commit -m init", clone);
            RunGit("branch -M main", clone);
            RunGit("push -u origin main", clone);
            RunGit("symbolic-ref HEAD refs/heads/main", origin);

            File.WriteAllText(serverPath, "{\"landingFtp\":\"ftp://ftp:ftp123@4tube.xyz/wwwroot/4tube.xyz/\"}\n");

            var other = Path.Combine(root, "other");
            RunGit($"clone --branch main \"{origin}\" \"{other}\"", root);
            RunGit("config user.email test@local", other);
            RunGit("config user.name Test", other);
            File.WriteAllText(
                Path.Combine(other, "gonzik", "server", "server.json"),
                "{\"landingFtp\":\"from-other-host\"}\n");
            RunGit("add -A", other);
            RunGit("commit -m remote-change", other);
            RunGit("push origin main", other);

            HephaestusDataGitRunner.SyncExistingRepository(clone, NullLogger.Instance);

            File.ReadAllText(serverPath).Should().Contain("4tube.xyz");
            RunGit("clone \"origin.git\" pulled", root);
            File.ReadAllText(Path.Combine(root, "pulled", "gonzik", "server", "server.json"))
                .Should().Contain("4tube.xyz");
        }
        finally
        {
            try
            {
                foreach (var info in new DirectoryInfo(root).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    info.Attributes = FileAttributes.Normal;
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static void RunGit(string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git failed to start");
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {arguments} failed: {stderr}");
    }
}
