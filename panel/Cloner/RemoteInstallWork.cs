using System.Threading.Channels;

namespace Cloner;

public sealed record RemoteInstallWork(
    Guid RunId,
    string Profile,
    string Host,
    string User,
    string Password,
    ChannelWriter<string> LogWriter,
    CancellationToken RunCancellationToken);
