using InstallRemote;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var credsPath = RemoteInstallCredsFile.ResolveCredsPath(AppContext.BaseDirectory);
        var creds = RemoteInstallCredsFile.LoadFromPathOrThrow(credsPath);
        var server = args.Length > 0 ? args[0].Trim() : creds.Server;
        var login = args.Length > 1 ? args[1].Trim() : creds.Login;
        var password = args.Length > 2 ? args[2] : creds.Password;

        try
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, RemoteInstallRunner.DefaultRemoteScriptFileName);
            var remoteCmd = RemoteInstallRunner.LoadRemoteInstallBootstrapScript(scriptPath);

            Console.WriteLine($"Remote install -> {login}@{server}");
            Console.WriteLine("[1/1] SSH: install git, clone repo to $HOME/hephaestus (remote user), run install.sh");

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
}
