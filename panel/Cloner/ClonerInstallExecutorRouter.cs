using Microsoft.Extensions.Options;

namespace Cloner;

internal sealed class ClonerInstallExecutorRouter : IClonerInstallExecutor
{
    private readonly IOptionsMonitor<ClonerOptions> _options;
    private readonly InProcessClonerInstallExecutor _inProcess;
    private readonly HttpDomainHostInstallRemoteExecutor _http;

    public ClonerInstallExecutorRouter(
        IOptionsMonitor<ClonerOptions> options,
        InProcessClonerInstallExecutor inProcess,
        HttpDomainHostInstallRemoteExecutor http)
    {
        _options = options;
        _inProcess = inProcess;
        _http = http;
    }

    public Task<int> ExecuteAsync(RemoteInstallJob job, CancellationToken cancellationToken)
    {
        if (string.Equals(_options.CurrentValue.Executor, "DomainHostHttp", StringComparison.OrdinalIgnoreCase))
            return _http.ExecuteAsync(job, cancellationToken);
        return _inProcess.ExecuteAsync(job, cancellationToken);
    }
}
