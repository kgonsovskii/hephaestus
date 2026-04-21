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
    private readonly Channel<bool> _troyanWake = CreateChannel();
    private readonly Channel<bool> _landingWake = CreateChannel();

    /// <summary>Wakes domain DNS refiner, domain catalog refresh, Troyan build maintenance, and landing FTP maintenance (CP apply / domain save).</summary>
    public void NotifyHostsChanged()
    {
        _refinerWake.Writer.TryWrite(true);
        _catalogWake.Writer.TryWrite(true);
        _troyanWake.Writer.TryWrite(true);
        _landingWake.Writer.TryWrite(true);
    }

    public Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default) =>
        _refinerWake.Reader.ReadAsync(cancellationToken).AsTask();

    public Task WhenCatalogWakeAsync(CancellationToken cancellationToken = default) =>
        _catalogWake.Reader.ReadAsync(cancellationToken).AsTask();

    public Task WhenTroyanWakeAsync(CancellationToken cancellationToken = default) =>
        _troyanWake.Reader.ReadAsync(cancellationToken).AsTask();

    public Task WhenLandingWakeAsync(CancellationToken cancellationToken = default) =>
        _landingWake.Reader.ReadAsync(cancellationToken).AsTask();

    public void DrainExtraRefinerSignals()
    {
        while (_refinerWake.Reader.TryRead(out _)) { }
    }

    public void DrainExtraCatalogSignals()
    {
        while (_catalogWake.Reader.TryRead(out _)) { }
    }

    public void DrainExtraTroyanSignals()
    {
        while (_troyanWake.Reader.TryRead(out _)) { }
    }

    public void DrainExtraLandingSignals()
    {
        while (_landingWake.Reader.TryRead(out _)) { }
    }
}
