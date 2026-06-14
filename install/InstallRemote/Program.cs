using InstallRemote;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var argOffset = 0;
        string profile;
        try
        {
            var repoRoot = ResolveRepoRoot(AppContext.BaseDirectory);
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                profile = ValidateProfileName(args[0]);
                WriteProfileFile(repoRoot, profile);
                argOffset = 1;
            }
            else
            {
                profile = ResolveProfile(repoRoot);
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }

        var credsPath = RemoteInstallCredsFile.ResolveCredsPath(AppContext.BaseDirectory);
        var creds = RemoteInstallCredsFile.LoadFromPathOrThrow(credsPath);
        var server = args.Length > argOffset ? args[argOffset].Trim() : creds.Server;
        var login = args.Length > argOffset + 1 ? args[argOffset + 1].Trim() : creds.Login;
        var password = args.Length > argOffset + 2 ? args[argOffset + 2] : creds.Password;

        try
        {
            var repoRoot = ResolveRepoRoot(AppContext.BaseDirectory);
            var scriptPath = Path.Combine(repoRoot, "install", "shared", RemoteInstallRunner.DefaultRemoteScriptFileName);
            var remoteCmd = RemoteInstallRunner.PrependProfileExport(
                profile,
                RemoteInstallRunner.LoadRemoteInstallBootstrapScript(scriptPath));

            Console.WriteLine($"Remote install -> {login}@{server} (profile {profile})");
            Console.WriteLine("[1/1] SSH: write profile.txt, clone repo to $HOME/hephaestus (remote user), run install.sh");

            var sshpass = await SshPassBootstrap.EnsureAsync(msg => Console.WriteLine(msg), default).ConfigureAwait(false);

            var code = await RemoteInstallRunner.RunRemoteInstallAsync(
                    sshpass,
                    server,
                    login,
                    password,
                    remoteCmd,
                    async (line, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        await Console.Out.WriteAsync(line + Environment.NewLine).ConfigureAwait(false);
                    },
                    onProcessStarted: null,
                    cancellationToken: default)
                .ConfigureAwait(false);

            if (code != 0)
            {
                await Console.Error.WriteLineAsync($"Remote install failed with exit {code}").ConfigureAwait(false);
                return code;
            }

            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }
    }

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

    private static string ResolveProfile(string repositoryRoot)
    {
        var env = Environment.GetEnvironmentVariable("HEPHAESTUS_PROFILE");
        if (!string.IsNullOrWhiteSpace(env))
            return ValidateProfileName(env);

        var path = ResolveProfileFilePath(repositoryRoot);
        if (File.Exists(path))
        {
            var line = File.ReadLines(path).FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(line))
                return ValidateProfileName(line);
        }

        return "default";
    }

    private static string ResolveProfileFilePath(string repositoryRoot)
    {
        var parent = Directory.GetParent(Path.GetFullPath(repositoryRoot))?.FullName
            ?? throw new InvalidOperationException($"Cannot resolve profile file beside repository root '{repositoryRoot}'.");
        return Path.Combine(parent, "profile.txt");
    }

    private static string ValidateProfileName(string value)
    {
        var profile = value.Trim().Trim('\\', '/');
        if (string.IsNullOrWhiteSpace(profile) || profile is "." or ".."
            || profile.Contains('/') || profile.Contains('\\'))
        {
            throw new ArgumentException($"Invalid profile name: '{value}'");
        }

        return profile;
    }

    private static void WriteProfileFile(string repositoryRoot, string profileName)
    {
        var profile = ValidateProfileName(profileName);
        var path = ResolveProfileFilePath(repositoryRoot);
        File.WriteAllText(path, profile + Environment.NewLine);
        Console.WriteLine($"[install] Wrote profile '{profile}' to {path}");
    }
}
