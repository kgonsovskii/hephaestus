using System.Threading.Channels;

namespace Cloner;

public sealed record RemoteInstallWork(
    Guid RunId,
    string Host,
    ChannelWriter<string> LogWriter,
    CancellationToken RunCancellationToken);
