using System.Diagnostics;
using System.Text;
using Commons;
using Microsoft.Extensions.Logging;

namespace Git;

public static class HephaestusDataGitRunner
{
    public static void Run(IHephaestusPathResolver paths, ILogger logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataDir = paths.ResolveHephaestusDataBase(AppContext.BaseDirectory);
        var gitDir = Path.Combine(dataDir, ".git");

        if (Directory.Exists(gitDir))
        {
            logger.LogTrace(
                "Hephaestus data git: repository present at {DataDir}; sync stub (nothing to do).",
                dataDir);
            return;
        }

        if (Directory.Exists(dataDir))
        {
            logger.LogInformation("Hephaestus data git: removing non-repository directory {DataDir}.", dataDir);
            Directory.Delete(dataDir, recursive: true);
        }

        var parent = Path.GetDirectoryName(dataDir);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        logger.LogInformation(
            "Hephaestus data git: cloning {RepositoryUrl} into {DataDir}.",
            HephaestusDataGitConstants.RepositoryUrl,
            dataDir);

        RunGit($"clone \"{HephaestusDataGitConstants.CloneUrl}\" \"{dataDir}\"", workingDirectory: null, logger);
        logger.LogInformation("Hephaestus data git: clone finished.");
    }

    private static void RunGit(string arguments, string? workingDirectory, ILogger logger)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
            throw new InvalidOperationException("Failed to start git process.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
            logger.LogDebug("git stdout: {Output}", stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr))
            logger.LogDebug("git stderr: {Output}", stderr.Trim());

        if (process.ExitCode != 0)
        {
            var detail = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(stderr))
                detail.Append(stderr.Trim());
            else if (!string.IsNullOrWhiteSpace(stdout))
                detail.Append(stdout.Trim());
            else
                detail.Append($"exit code {process.ExitCode}");

            throw new InvalidOperationException($"git {arguments} failed: {detail}");
        }
    }
}
