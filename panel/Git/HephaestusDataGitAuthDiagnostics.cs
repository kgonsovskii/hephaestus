using System.Diagnostics;
using Commons;

namespace Git;

/// <summary>Live checks against GitHub for <see cref="HephaestusDataGitConstants"/> (read vs push).</summary>
public static class HephaestusDataGitAuthDiagnostics
{
    public sealed class StepResult
    {
        public required string Name { get; init; }

        public required bool Success { get; init; }

        public required string Detail { get; init; }
    }

    public sealed class Report
    {
        public required string TokenFingerprint { get; init; }

        public required string RepositoryUrl { get; init; }

        public string? DataDirectory { get; init; }

        public required StepResult ReadRemote { get; init; }

        public required StepResult PushDryRun { get; init; }

        public bool CanRead => ReadRemote.Success;

        public bool CanPush => PushDryRun.Success;

        public override string ToString() =>
            $"""
             Hephaestus data Git PAT diagnostic
             Repository: {RepositoryUrl}
             Data directory: {DataDirectory ?? "(temp clone)"}
             Token fingerprint: {TokenFingerprint}
             Read (git ls-remote): {(CanRead ? "OK" : "FAIL")}
               {ReadRemote.Detail}
             Push (git push --dry-run from data dir): {(CanPush ? "OK" : "FAIL")}
               {PushDryRun.Detail}
             """;

        public string FailureHint =>
            CanPush
                ? string.Empty
                : CanRead
                    ? "Read works but push failed: fine-grained PAT needs repository kgonsovskii/hephaestus_data selected and Contents read+write."
                    : "Read failed: token missing, revoked, or repo not granted on the PAT.";
    }

    public static Report Diagnose()
    {
        var dataDir = TryResolveDataDirectory();
        var read = TestReadRemote();
        var push = read.Success ? TestPushDryRun(dataDir) : SkipPushBecauseReadFailed(read.Detail);
        return new Report
        {
            TokenFingerprint = HephaestusDataGitConstants.TokenFingerprint,
            RepositoryUrl = HephaestusDataGitConstants.RepositoryUrl,
            DataDirectory = dataDir,
            ReadRemote = read,
            PushDryRun = push
        };
    }

    private static string? TryResolveDataDirectory()
    {
        foreach (var baseDir in CandidateAppBaseDirectories())
        {
            try
            {
                var paths = HephaestusPathResolver.FromAppSettingsInDirectory(baseDir);
                var dataDir = paths.ResolveHephaestusDataBase(baseDir);
                if (Directory.Exists(Path.Combine(dataDir, ".git")))
                    return dataDir;
            }
            catch
            {
                // try next candidate base directory
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateAppBaseDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var full = Path.GetFullPath(path);
            return seen.Add(full) ? full : null;
        }

        if (Add(AppContext.BaseDirectory) is { } appBase)
            yield return appBase;

        var dir = Path.GetFullPath(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent is null)
                break;
            dir = parent;

            if (Add(Path.Combine(dir, "output")) is { } outputDir)
                yield return outputDir;

            if (Add(Path.Combine(dir, "release")) is { } releaseDir)
                yield return releaseDir;

            if (File.Exists(Path.Combine(dir, "appsettings.json")) && Add(dir) is { } settingsDir)
                yield return settingsDir;
        }
    }

    private static StepResult SkipPushBecauseReadFailed(string readDetail) =>
        new()
        {
            Name = "push-dry-run",
            Success = false,
            Detail = $"skipped (read failed first): {readDetail}"
        };

    private static StepResult TestReadRemote()
    {
        var args = $"{NetworkGitConfig()}ls-remote \"{HephaestusDataGitConstants.CloneUrl}\" HEAD";
        var result = ExecuteGit(args, workingDirectory: null);
        return new StepResult
        {
            Name = "read-remote",
            Success = result.Success,
            Detail = result.Detail
        };
    }

    private static StepResult TestPushDryRun(string? dataDirectory)
    {
        if (!string.IsNullOrEmpty(dataDirectory))
            return TestPushDryRunFromExistingRepo(dataDirectory);

        return TestPushDryRunFromTempClone();
    }

    private static StepResult TestPushDryRunFromExistingRepo(string dataDirectory)
    {
        RefreshOrigin(dataDirectory);
        var pushArgs = $"{NetworkGitConfig()}push --dry-run origin HEAD";
        var push = ExecuteGit(pushArgs, workingDirectory: dataDirectory);
        return new StepResult
        {
            Name = "push-dry-run",
            Success = push.Success,
            Detail = push.Detail
        };
    }

    private static StepResult TestPushDryRunFromTempClone()
    {
        var temp = Path.Combine(Path.GetTempPath(), "hephaestus-git-diag-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cloneArgs = $"{NetworkGitConfig()}clone --depth 1 \"{HephaestusDataGitConstants.CloneUrl}\" \"{temp}\"";
            var clone = ExecuteGit(cloneArgs, workingDirectory: null);
            if (!clone.Success)
            {
                return new StepResult
                {
                    Name = "push-dry-run",
                    Success = false,
                    Detail = $"clone before push probe failed: {clone.Detail}"
                };
            }

            return TestPushDryRunFromExistingRepo(temp);
        }
        finally
        {
            try
            {
                if (Directory.Exists(temp))
                    Directory.Delete(temp, recursive: true);
            }
            catch
            {
                // best-effort temp cleanup
            }
        }
    }

    private static void RefreshOrigin(string dataDirectory)
    {
        var url = HephaestusDataGitConstants.CloneUrl;
        if (ExecuteGit("remote get-url origin", dataDirectory).Success)
            ExecuteGit($"{NetworkGitConfig()}remote set-url origin \"{url}\"", dataDirectory);
        else
            ExecuteGit($"{NetworkGitConfig()}remote add origin \"{url}\"", dataDirectory);
    }

    private static string NetworkGitConfig() =>
        "-c credential.helper= -c core.askPass= -c credential.useHttpPath=true "
        + (OperatingSystem.IsWindows() ? "-c credential.helperManager= " : "");

    private static GitResult ExecuteGit(string arguments, string? workingDirectory)
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
        return new GitResult(process.ExitCode, stdout, stderr);
    }

    private readonly struct GitResult(int exitCode, string stdout, string stderr)
    {
        public bool Success => exitCode == 0;

        public string Detail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(stderr))
                    return stderr.Trim();
                if (!string.IsNullOrWhiteSpace(stdout))
                    return stdout.Trim();
                return $"exit code {exitCode}";
            }
        }
    }
}
