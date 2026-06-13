using System.Diagnostics;
using Commons;
using Microsoft.Extensions.Logging;

namespace Git;

public static class HephaestusDataGitRunner
{
    private const string SyncStashMessage = "hephaestus-pre-sync";
    private const string SyncCommitMessage = "Hephaestus server sync";
    private const string NetworkGitConfig =
        "-c credential.helper= -c credential.helperManager= -c core.askPass= ";

    private static readonly SemaphoreSlim SyncGate = new(1, 1);

    public static void Run(IHephaestusPathResolver paths, ILogger logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SyncGate.Wait(cancellationToken);
        try
        {
            RunCore(paths, logger, cancellationToken);
        }
        finally
        {
            SyncGate.Release();
        }
    }

    private static void RunCore(IHephaestusPathResolver paths, ILogger logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataDir = paths.ResolveHephaestusDataBase(AppContext.BaseDirectory);
        var gitDir = Path.Combine(dataDir, ".git");

        if (!Directory.Exists(gitDir))
        {
            CloneFresh(dataDir, logger);
            return;
        }

        SyncExistingRepository(dataDir, logger, cancellationToken);
    }

    private static void CloneFresh(string dataDir, ILogger logger)
    {
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

        RunGit($"{NetworkGitConfig}clone \"{HephaestusDataGitConstants.CloneUrl}\" \"{dataDir}\"", workingDirectory: null, logger);
        EnsureGitIdentity(dataDir, logger);
        logger.LogInformation("Hephaestus data git: clone finished.");
    }

    private static void SyncExistingRepository(string dataDir, ILogger logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureGitIdentity(dataDir, logger);
        RefreshAuthenticatedRemote(dataDir, logger);

        var stashed = TryStashLocalChanges(dataDir, logger);

        RunGit($"{NetworkGitConfig}fetch origin", dataDir, logger);
        var branch = ResolveTrackingBranch(dataDir, logger);
        logger.LogInformation("Hephaestus data git: pulling origin/{Branch} (remote wins on conflicts).", branch);
        PullPreferringRemote(dataDir, branch, logger);

        if (stashed)
            RestoreStash(dataDir, logger);

        cancellationToken.ThrowIfCancellationRequested();
        PushLocalChanges(dataDir, branch, logger);
    }

    private static bool TryStashLocalChanges(string dataDir, ILogger logger)
    {
        if (!HasWorkingTreeChanges(dataDir, logger))
            return false;

        logger.LogDebug("Hephaestus data git: stashing local changes before pull.");
        if (!TryRunGit($"stash push -u -m \"{SyncStashMessage}\"", dataDir, logger, out var stashError))
        {
            logger.LogDebug("Hephaestus data git: stash skipped ({Error}).", stashError);
            return false;
        }

        if (!HasStashEntries(dataDir, logger))
        {
            logger.LogDebug("Hephaestus data git: stash push reported success but stash list is empty.");
            return false;
        }

        return true;
    }

    private static void PullPreferringRemote(string dataDir, string branch, ILogger logger)
    {
        var pullArgs = $"{NetworkGitConfig}pull origin {branch} --no-rebase --no-edit -X theirs";
        if (TryRunGit(pullArgs, dataDir, logger, out _))
        {
            logger.LogInformation("Hephaestus data git: pull finished (remote wins on conflicts).");
            return;
        }

        logger.LogWarning("Hephaestus data git: pull failed; hard-resetting to origin/{Branch}.", branch);
        AbortMergeIfInProgress(dataDir, logger);
        RunGit($"reset --hard origin/{branch}", dataDir, logger);
        TryRunGit("clean -fd", dataDir, logger, out _);
        logger.LogInformation("Hephaestus data git: hard reset to origin/{Branch} finished.", branch);
    }

    private static void RestoreStash(string dataDir, ILogger logger)
    {
        if (!HasStashEntries(dataDir, logger))
        {
            logger.LogTrace("Hephaestus data git: no stash to restore.");
            return;
        }

        if (TryRunGit("stash pop", dataDir, logger, out _))
            return;

        logger.LogWarning(
            "Hephaestus data git: stash pop had conflicts; keeping stashed server versions.");
        TryRunGit("checkout --theirs -- .", dataDir, logger, out _);
        RunGit("add -A", dataDir, logger);
        TryRunGit("reset --quiet", dataDir, logger, out _);
        if (HasStashEntries(dataDir, logger))
            TryRunGit("stash drop", dataDir, logger, out _);
    }

    private static void PushLocalChanges(string dataDir, string branch, ILogger logger)
    {
        RunGit("add -A", dataDir, logger);

        if (HasStagedChanges(dataDir, logger))
        {
            if (!TryRunGit($"commit -m \"{SyncCommitMessage}\"", dataDir, logger, out var commitError)
                && !IsNothingToCommit(commitError))
            {
                throw new InvalidOperationException($"git commit failed: {commitError}");
            }
        }
        else
        {
            logger.LogTrace("Hephaestus data git: nothing to commit.");
        }

        if (!HasUnpushedCommits(dataDir, branch, logger))
        {
            logger.LogTrace("Hephaestus data git: nothing to push.");
            return;
        }

        var pushUrl = HephaestusDataGitConstants.CloneUrl;
        var pushArgs = $"{NetworkGitConfig}push \"{pushUrl}\" {branch}";
        if (!TryRunGit(pushArgs, dataDir, logger, out var pushError))
        {
            if (IsAuthFailure(pushError))
            {
                logger.LogWarning(
                    "Hephaestus data git: push skipped — GitHub rejected the token (check HephaestusDataGitConstants.AccessToken has Contents read+write on hephaestus_data). {Error}",
                    pushError);
                return;
            }

            throw new InvalidOperationException($"git push failed: {pushError}");
        }

        logger.LogInformation("Hephaestus data git: push to origin/{Branch} finished.", branch);
    }

    private static bool IsAuthFailure(string detail) =>
        detail.Contains("Invalid username or token", StringComparison.OrdinalIgnoreCase)
        || detail.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase)
        || detail.Contains("403", StringComparison.OrdinalIgnoreCase);

    private static bool IsNothingToCommit(string detail) =>
        detail.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase)
        || detail.Contains("nothing added to commit", StringComparison.OrdinalIgnoreCase);

    private static void EnsureGitIdentity(string dataDir, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(ReadGitConfigValue(dataDir, "user.email", logger)))
            RunGit("config user.email hephaestus@local", dataDir, logger);
        if (string.IsNullOrWhiteSpace(ReadGitConfigValue(dataDir, "user.name", logger)))
            RunGit("config user.name Hephaestus", dataDir, logger);
    }

    private static void RefreshAuthenticatedRemote(string dataDir, ILogger logger)
    {
        var url = HephaestusDataGitConstants.CloneUrl;
        if (TryRunGit("remote get-url origin", dataDir, logger, out _))
            RunGit($"{NetworkGitConfig}remote set-url origin \"{url}\"", dataDir, logger);
        else
            RunGit($"{NetworkGitConfig}remote add origin \"{url}\"", dataDir, logger);
    }

    private static string ResolveTrackingBranch(string dataDir, ILogger logger)
    {
        if (TryRunGit("symbolic-ref --short refs/remotes/origin/HEAD", dataDir, logger, out var originHead)
            && !string.IsNullOrWhiteSpace(originHead))
        {
            const string prefix = "origin/";
            if (originHead.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return originHead[prefix.Length..];
        }

        if (TryRunGit("rev-parse --abbrev-ref HEAD", dataDir, logger, out var head)
            && !string.IsNullOrWhiteSpace(head)
            && !string.Equals(head, "HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return head;
        }

        logger.LogDebug("Hephaestus data git: defaulting tracking branch to main.");
        return "main";
    }

    private static bool HasWorkingTreeChanges(string dataDir, ILogger logger)
    {
        if (!TryRunGit("status --porcelain", dataDir, logger, out var status))
            return false;
        return !string.IsNullOrWhiteSpace(status);
    }

    private static bool HasStagedChanges(string dataDir, ILogger logger)
    {
        var result = ExecuteGit("diff --cached --quiet", dataDir, logger);
        return result.ExitCode == 1;
    }

    private static bool HasStashEntries(string dataDir, ILogger logger)
    {
        return TryRunGit("stash list", dataDir, logger, out var list) && !string.IsNullOrWhiteSpace(list);
    }

    private static bool HasUnpushedCommits(string dataDir, string branch, ILogger logger)
    {
        if (!TryRunGit($"rev-list --count origin/{branch}..HEAD", dataDir, logger, out var countText))
            return false;

        return int.TryParse(countText.Trim(), out var count) && count > 0;
    }

    private static void AbortMergeIfInProgress(string dataDir, ILogger logger)
    {
        if (File.Exists(Path.Combine(dataDir, ".git", "MERGE_HEAD")))
            TryRunGit("merge --abort", dataDir, logger, out _);
    }

    private static string? ReadGitConfigValue(string dataDir, string key, ILogger logger)
    {
        return TryRunGit($"config --get {key}", dataDir, logger, out var value) ? value.Trim() : null;
    }

    private static bool TryRunGit(string arguments, string? workingDirectory, ILogger logger, out string detail)
    {
        var result = ExecuteGit(arguments, workingDirectory, logger);
        detail = result.Detail;
        return result.Success;
    }

    private static void RunGit(string arguments, string? workingDirectory, ILogger logger)
    {
        var result = ExecuteGit(arguments, workingDirectory, logger);
        if (!result.Success)
            throw new InvalidOperationException($"git {arguments} failed: {result.Detail}");
    }

    private static GitResult ExecuteGit(string arguments, string? workingDirectory, ILogger logger)
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
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        psi.Environment["GCM_INTERACTIVE"] = "Never";
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

        return new GitResult(process.ExitCode, stdout, stderr);
    }

    private readonly struct GitResult
    {
        public GitResult(int exitCode, string stdout, string stderr)
        {
            ExitCode = exitCode;
            Stdout = stdout;
            Stderr = stderr;
        }

        public int ExitCode { get; }

        public string Stdout { get; }

        public string Stderr { get; }

        public bool Success => ExitCode == 0;

        public string Detail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Stderr))
                    return Stderr.Trim();
                if (!string.IsNullOrWhiteSpace(Stdout))
                    return Stdout.Trim();
                return $"exit code {ExitCode}";
            }
        }
    }
}
