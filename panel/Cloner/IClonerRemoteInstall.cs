using System.Threading.Channels;

namespace Cloner;

public interface IClonerRemoteInstall
{
    /// <summary>Starts a remote install (HTTP to DomainHost <c>/internal/install-remote</c>); work runs on a background service. Only one install at a time; returns id for <see cref="TrySubscribeLogReader"/> / WebSocket.</summary>
    Task<Guid> StartRemoteInstallAsync(string host, CancellationToken cancellationToken = default);

    /// <summary>Log lines for an active or recently finished run; null if unknown run id.</summary>
    ChannelReader<string>? TrySubscribeLogReader(Guid runId);

    /// <summary>Cancels the running install for <paramref name="runId"/>; returns false if the id is unknown or already finished.</summary>
    bool TryStop(Guid runId);
}
