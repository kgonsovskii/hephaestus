using System.Threading.Channels;

namespace Cloner;

/// <summary>One in-flight remote install: parameters, log sink, and cancellation for that run.</summary>
public sealed record RemoteInstallWork(
    Guid RunId,
    string Host,
    string User,
    string Password,
    ChannelWriter<string> LogWriter,
    CancellationToken RunCancellationToken);
