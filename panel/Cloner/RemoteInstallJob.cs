using System.Threading.Channels;

namespace Cloner;

internal sealed record RemoteInstallJob(
    Guid RunId,
    string Host,
    string User,
    string Password,
    ChannelWriter<string> LogWriter);
