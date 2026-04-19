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
    private readonly Channel<bool> _catalogWake = CreateChannel();

    public void NotifyHostsChanged()
    {
        _refinerWake.Writer.TryWrite(true);
        _catalogWake.Writer.TryWrite(true);
    }

    public Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default) =>
        _refinerWake.Reader.ReadAsync(cancellationToken).AsTask();

    public Task WhenCatalogWakeAsync(CancellationToken cancellationToken = default) =>
        _catalogWake.Reader.ReadAsync(cancellationToken).AsTask();

    public void DrainExtraRefinerSignals()
    {
        while (_refinerWake.Reader.TryRead(out _)) { }
    }

    public void DrainExtraCatalogSignals()
    {
        while (_catalogWake.Reader.TryRead(out _)) { }
    }
}
