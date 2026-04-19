using System.Threading.Channels;

namespace Cloner;

public interface IClonerRemoteInstall
{
    /// <summary>Queues remote install (HTTP to DomainHost <c>/internal/install-remote</c>; no local SSH process on the panel host). Returns id for <see cref="TrySubscribeLogReader"/> / WebSocket.</summary>
    Task<Guid> StartRemoteInstallAsync(string host, string user, string password, CancellationToken cancellationToken = default);

    /// <summary>Log lines for an active or recently finished run; null if unknown run id.</summary>
    ChannelReader<string>? TrySubscribeLogReader(Guid runId);

    /// <summary>Cancels a queued or running install for <paramref name="runId"/>; returns false if the id is unknown or already finished.</summary>
    bool TryStop(Guid runId);
}
