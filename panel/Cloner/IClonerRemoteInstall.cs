using System.Threading.Channels;

namespace Cloner;

public interface IClonerRemoteInstall
{
    /// <summary>Queues install-remote (bat on Windows, sh on Linux). Returns id for <see cref="TrySubscribeLogReader"/> / WebSocket.</summary>
    Task<Guid> StartRemoteInstallAsync(string host, string user, string password, CancellationToken cancellationToken = default);

    /// <summary>Log lines for an active or recently finished run; null if unknown run id.</summary>
    ChannelReader<string>? TrySubscribeLogReader(Guid runId);
}
