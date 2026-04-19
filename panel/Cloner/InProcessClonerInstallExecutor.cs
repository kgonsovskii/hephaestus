using InstallRemote;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cloner;

internal sealed class InProcessClonerInstallExecutor : IClonerInstallExecutor
{
    private readonly ClonerRemoteInstallService _coordinator;
    private readonly IOptionsMonitor<ClonerOptions> _options;
    private readonly ILogger<InProcessClonerInstallExecutor> _logger;

    public InProcessClonerInstallExecutor(
        ClonerRemoteInstallService coordinator,
        IOptionsMonitor<ClonerOptions> options,
        ILogger<InProcessClonerInstallExecutor> logger)
    {
        _coordinator = coordinator;
        _options = options;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RemoteInstallJob job, CancellationToken cancellationToken)
    {
        var repoRoot = RepoRootResolver.Resolve(_options.CurrentValue.RepoRoot, _logger);
        var scriptPath = Path.Combine(repoRoot, "install", RemoteInstallRunner.DefaultRemoteScriptFileName);
        var script = RemoteInstallRunner.LoadRemoteScriptFromFile(scriptPath);

        var sshpass = RemoteInstallRunner.FindSshPassOnPath()
                      ?? throw new InvalidOperationException(
                          "sshpass not found on PATH. On Linux install: apt install sshpass (or equivalent).");

        return await RemoteInstallRunner.RunRemoteInstallAsync(
                sshpass,
                job.Host,
                job.User,
                job.Password,
                script,
                async (line, ct) => await job.LogWriter.WriteAsync(line, ct).ConfigureAwait(false),
                p => _coordinator.AttachRunningProcess(job.RunId, p),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
