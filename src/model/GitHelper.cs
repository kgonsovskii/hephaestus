using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace model;

public class WriterX : IOutputWriter
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}

public interface IOutputWriter
{
    void WriteLine(string message);
}

public static class GitHelper
{
    private static bool IsValidGitRepository(IOutputWriter context, string localPath, string expectedBranch)
    {
        if (!Directory.Exists(localPath) || !Directory.Exists(Path.Combine(localPath, ".git")))
        {
            return false;
        }
        try
        {
            var currentBranch = RunGitCommand(context, $"-C \"{localPath}\" rev-parse --abbrev-ref HEAD").Trim();
            if (currentBranch != expectedBranch)
            {
                context.WriteLine($"Current branch ({currentBranch}) does not match expected ({expectedBranch}).");
                return false;
            }
        }
        catch (Exception ex)
        {
            context.WriteLine($"Git repository validation failed: {ex.Message}");
            return false;
        }
        return true;
    }
    
    public static void CloneAndCheckout(IOutputWriter context,
        string repoUrl,
        string localPath,
        string branchName = "main")
    {
        if (IsValidGitRepository(context, localPath, branchName))
        {
            context.WriteLine($"Repository already exists at {localPath}. Checking out branch: {branchName}");

            RunGitCommand(context, $"-C \"{localPath}\" fetch --all");

            var branchExists = RunGitCommand(context, $"-C \"{localPath}\" branch --list {branchName}").Trim()
                .Replace("* ", "");
            if (string.IsNullOrEmpty(branchExists))
            {
                context.WriteLine($"Branch {branchName} does not exist. Creating and pushing it.");
                RunGitCommand(context, $"-C \"{localPath}\" checkout -b {branchName}");
                RunGitCommand(context, $"-C \"{localPath}\" push --set-upstream origin {branchName}");
            }
            else
            {
                RunGitCommand(context, $"-C \"{localPath}\" checkout {branchName}");
            }

            RunGitCommand(context, $"-C \"{localPath}\" pull");
        }
        else
        {
            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }

            context.WriteLine($"Cloning repository from {repoUrl} to {localPath}");
            Directory.CreateDirectory(localPath);
            RunGitCommand(context, $"clone {repoUrl} \"{localPath}\"");

            RunGitCommand(context, $"-C \"{localPath}\" fetch --all");

            var branchExistsRemote =
                RunGitCommand(context, $"-C \"{localPath}\" ls-remote --heads origin {branchName}")
                    .Trim();
            if (string.IsNullOrEmpty(branchExistsRemote))
            {
                context.WriteLine($"Branch {branchName} does not exist on remote. Creating and pushing it.");
                RunGitCommand(context, $"-C \"{localPath}\" checkout -b {branchName}");
                RunGitCommand(context, $"-C \"{localPath}\" push --set-upstream origin {branchName}");
            }
            else
            {
                RunGitCommand(context, $"-C \"{localPath}\" checkout {branchName}");
            }
        }

        context.WriteLine($"Successfully switched to branch: {branchName}");
    }

    public static void CommitAndPush(IOutputWriter context, string localPath, string version)
    {
        RunGitCommand(context, $"-C \"{localPath}\" config user.name \"FakeUser\"");
        RunGitCommand(context, $"-C \"{localPath}\" config user.email \"fakeuser@example.com\"");
        RunGitCommand(context, $"-C \"{localPath}\" add .");
        try
        {
            RunGitCommand(context, $"-C \"{localPath}\" commit -m \"{version}\"");
        }
        catch {
            //nothing
        }
        RunGitCommand(context, $"-C \"{localPath}\" push --force");
    }

    private static string RunGitCommand(IOutputWriter? context, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "git",
            Arguments              = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.Start();

        var output = process.StandardOutput.ReadToEnd().Trim();
        var error  = process.StandardError.ReadToEnd().Trim();

        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            context?.WriteLine($"Git Error: {error}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed with exit code {process.ExitCode}: {error}");
        }

        context?.WriteLine(output);
        return output;
    }

    internal static void DeleteDirectoryForcefully(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(file);

            if ((attributes & FileAttributes.ReadOnly) != 0 || (attributes & FileAttributes.Hidden) != 0)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            File.Delete(file);
        }

        foreach (var dir in Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(dir);

            if ((attributes & FileAttributes.ReadOnly) != 0 || (attributes & FileAttributes.Hidden) != 0)
            {
                File.SetAttributes(dir, FileAttributes.Normal);
            }
        }

        Directory.Delete(folderPath, true);
    }
}