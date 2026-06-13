using System.Threading.Channels;

namespace Domain;

public sealed class DomainHostsChangedSignal : IDomainHostsChangedSignal
{
    private static Channel<bool> CreateChannel() =>
        Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private readonly Channel<bool> _refinerWake = CreateChannel();
    private readonly Channel<bool> _troyanWake = CreateChannel();

    /// <summary>Wakes domain maintenance (catalog + Technitium DNS) and Troyan build (+ landing FTP after build).</summary>
    public void NotifyHostsChanged()
    {
        _refinerWake.Writer.TryWrite(true);
        _troyanWake.Writer.TryWrite(true);
    }

    public Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default) =>
        _refinerWake.Reader.ReadAsync(cancellationToken).AsTask();

    public Task WhenTroyanWakeAsync(CancellationToken cancellationToken = default) =>
        _troyanWake.Reader.ReadAsync(cancellationToken).AsTask();

    public void DrainExtraRefinerSignals()
    {
        while (_refinerWake.Reader.TryRead(out _)) { }
    }

    public void DrainExtraTroyanSignals()
    {
        while (_troyanWake.Reader.TryRead(out _)) { }
    }
}
