using InstallRemote;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var forceDns = args.Any(a => string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase));
        args = args.Where(a => !string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase)).ToArray();

        var argOffset = 0;
        string? cliProfile = null;
        try
        {
            var repoRoot = ResolveRepoRoot(AppContext.BaseDirectory);
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                cliProfile = RemoteInstallCredsFile.ValidateProfileName(args[0]);
                WriteProfileFile(repoRoot, cliProfile);
                argOffset = 1;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }

        var credsPath = RemoteInstallCredsFile.ResolveCredsPath(AppContext.BaseDirectory);
        IReadOnlyList<RemoteCreds> targets;
        try
        {
            var all = RemoteInstallCredsFile.LoadAllFromPathOrThrow(credsPath);
            targets = ResolveTargets(all, args, argOffset, cliProfile);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }

        try
        {
            var repoRoot = ResolveRepoRoot(AppContext.BaseDirectory);
            var scriptPath = Path.Combine(repoRoot, "install", "shared", RemoteInstallRunner.DefaultRemoteScriptFileName);
            var bootstrap = RemoteInstallRunner.LoadRemoteInstallBootstrapScript(scriptPath);

            Console.WriteLine($"Remote install: {targets.Count} server(s) in parallel (profile from creds, overwritten on each target)");
            foreach (var t in targets)
                Console.WriteLine($"  - {t.Login}@{t.Server}  profile {t.Profile}");
            Console.WriteLine("SSH: write $HOME/profile.txt, clone repo to $HOME/hephaestus, run install.sh");

            var sshpass = await SshPassBootstrap.EnsureAsync(msg => Console.WriteLine(msg), default).ConfigureAwait(false);

            var consoleLock = new object();
            var results = await RemoteInstallParallel.RunAsync(
                    sshpass,
                    targets,
                    creds =>
                    {
                        var script = RemoteInstallRunner.PrependProfileExport(creds.Profile, bootstrap);
                        return forceDns
                            ? "export HEPHAESTUS_FORCE_DNS=1\n" + script
                            : script;
                    },
                    (host, line, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        lock (consoleLock)
                            Console.Out.Write(PrefixHost(host, line) + Environment.NewLine);
                        return Task.CompletedTask;
                    },
                    cancellationToken: default)
                .ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine(RemoteInstallParallel.FormatReport(results));

            return results.All(r => r.Succeeded) ? 0 : 1;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }
    }

    private static IReadOnlyList<RemoteCreds> ResolveTargets(
        IReadOnlyList<RemoteCreds> fromFile,
        string[] args,
        int argOffset,
        string? cliProfile)
    {
        if (args.Length <= argOffset)
            return fromFile;

        var first = fromFile[0];
        var server = args[argOffset].Trim();
        var login = args.Length > argOffset + 1 ? args[argOffset + 1].Trim() : first.Login;
        var password = args.Length > argOffset + 2 ? args[argOffset + 2] : first.Password;
        var profile = cliProfile ?? first.Profile;
        return [new RemoteCreds(server, login, password, profile)];
    }

    private static string PrefixHost(string host, string line) => $"[{host}] {line}";

    private static string ResolveRepoRoot(string baseDirectory)
    {
        var dir = Path.GetFullPath(baseDirectory);
        for (var i = 0; i < 8; i++)
        {
            var install = Path.Combine(dir, "install");
            if (Directory.Exists(install)
                && File.Exists(Path.Combine(install, "shared", RemoteInstallRunner.DefaultRemoteScriptFileName)))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir)?.FullName;
            if (string.IsNullOrEmpty(parent))
                break;
            dir = parent;
        }

        throw new InvalidOperationException("Cannot resolve Hephaestus repository root (expected install/shared/install-remote.txt).");
    }

    private static string ResolveProfileFilePath(string repositoryRoot)
    {
        var parent = Directory.GetParent(Path.GetFullPath(repositoryRoot))?.FullName
            ?? throw new InvalidOperationException($"Cannot resolve profile file beside repository root '{repositoryRoot}'.");
        return Path.Combine(parent, "profile.txt");
    }

    private static void WriteProfileFile(string repositoryRoot, string profileName)
    {
        var profile = RemoteInstallCredsFile.ValidateProfileName(profileName);
        var path = ResolveProfileFilePath(repositoryRoot);
        File.WriteAllText(path, profile + Environment.NewLine);
        Console.WriteLine($"[install] Wrote local profile '{profile}' to {path} (remote hosts still use creds profiles unless a single host is passed)");
    }
}
