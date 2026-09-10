using System.Diagnostics;
using Commons;
using Microsoft.Extensions.Logging;

namespace Git;

public static class HephaestusDataGitRunner
{
    private const string SyncCommitMessage = "Hephaestus server sync";
    private static string NetworkGitConfig =>
        "-c credential.helper= -c core.askPass= -c credential.useHttpPath=true "
        + (OperatingSystem.IsWindows() ? "-c credential.helperManager= " : "");

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

    /// <summary>Used by tests against a throwaway clone. Commits local CP writes before merging origin; local wins conflicts; never hard-resets.</summary>
    internal static void SyncExistingRepository(string dataDir, ILogger logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureGitIdentity(dataDir, logger);
        RefreshAuthenticatedRemote(dataDir, logger);

        // Commit CP apply (and anything else) first so a later merge cannot throw it away.
        CommitWorkingTreeIfNeeded(dataDir, logger);

        RunGit($"{NetworkGitConfig}fetch origin", dataDir, logger);
        var branch = ResolveTrackingBranch(dataDir, logger);
        logger.LogInformation("Hephaestus data git: merging origin/{Branch} (local CP writes win on conflicts).", branch);
        if (!TryMergePreferringLocal(dataDir, branch, logger))
        {
            logger.LogWarning(
                "Hephaestus data git: merge of origin/{Branch} failed; keeping local commits (no hard reset).",
                branch);
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Pick up a CP save that landed during fetch/merge.
        PushLocalChanges(dataDir, branch, logger);
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

    private static bool TryMergePreferringLocal(string dataDir, string branch, ILogger logger)
    {
        var pullArgs = $"{NetworkGitConfig}pull origin {branch} --no-rebase --no-edit -X ours";
        if (TryRunGit(pullArgs, dataDir, logger, out _))
        {
            logger.LogInformation("Hephaestus data git: pull finished (local wins on conflicts).");
            return true;
        }

        AbortMergeIfInProgress(dataDir, logger);
        var mergeArgs = $"{NetworkGitConfig}merge origin/{branch} --no-edit -X ours";
        if (TryRunGit(mergeArgs, dataDir, logger, out _))
        {
            logger.LogInformation("Hephaestus data git: merge finished (local wins on conflicts).");
            return true;
        }

        AbortMergeIfInProgress(dataDir, logger);
        return false;
    }

    private static void CommitWorkingTreeIfNeeded(string dataDir, ILogger logger)
    {
        RunGit("add -A", dataDir, logger);
        if (!HasStagedChanges(dataDir, logger))
            return;

        if (TryRunGit($"commit -m \"{SyncCommitMessage}\"", dataDir, logger, out var commitError))
        {
            logger.LogInformation("Hephaestus data git: committed local changes.");
            return;
        }

        if (!IsNothingToCommit(commitError))
            throw new InvalidOperationException($"git commit failed: {commitError}");
    }

    private static void PushLocalChanges(string dataDir, string branch, ILogger logger)
    {
        CommitWorkingTreeIfNeeded(dataDir, logger);

        if (!HasUnpushedCommits(dataDir, branch, logger))
        {
            logger.LogInformation("Hephaestus data git: no push needed (already up to date with origin/{Branch}).", branch);
            return;
        }

        logger.LogInformation("Hephaestus data git: pushing from {DataDir} to origin/{Branch}.", dataDir, branch);
        var pushArgs = $"{NetworkGitConfig}push origin {branch}";
        if (!TryRunGit(pushArgs, dataDir, logger, out var pushError))
        {
            if (IsAuthFailure(pushError))
            {
                logger.LogWarning(
                    "Hephaestus data git: push skipped — GitHub rejected the token (PAT fingerprint {TokenFingerprint}; need Contents read+write on kgonsovskii/hephaestus_data and repo selected on the token). {Error}",
                    HephaestusDataGitConstants.TokenFingerprint,
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
        if (TryRunGit("remote get-url origin", dataDir, logger, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            // Keep throwaway remotes (tests). Refresh the PAT URL only for the data repo.
            if (!existing.Contains("hephaestus_data", StringComparison.OrdinalIgnoreCase))
                return;

            RunGit($"{NetworkGitConfig}remote set-url origin \"{url}\"", dataDir, logger);
            return;
        }

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

    private static bool HasStagedChanges(string dataDir, ILogger logger)
    {
        var result = ExecuteGit("diff --cached --quiet", dataDir, logger);
        return result.ExitCode == 1;
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
