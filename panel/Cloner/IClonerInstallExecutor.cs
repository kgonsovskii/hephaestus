namespace Cloner;

/// <summary>Runs one remote-install job (in-process SSH or HTTP to DomainHost).</summary>
public interface IClonerInstallExecutor
{
    Task<int> ExecuteAsync(RemoteInstallJob job, CancellationToken cancellationToken);
}
