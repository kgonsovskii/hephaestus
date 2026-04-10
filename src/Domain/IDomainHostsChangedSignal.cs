namespace Domain;

/// <summary>
/// Lets callers request an early run of hosted work that reacts to <c>domains.json</c> changes
/// (Technitium sync loop and <see cref="DomainCatalogRefreshService"/>).
/// Does not run work inline — only queues a wake so hosted loops skip the rest of their sleep.
/// </summary>
public interface IDomainHostsChangedSignal
{
    /// <summary>Wake Refiner domain maintenance and catalog refresh (each gets one signal).</summary>
    void NotifyHostsChanged();

    /// <summary>Refiner <c>RunDomainLoopWithWakeAsync</c> waits on this after each Technitium run.</summary>
    Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default);

    /// <summary><see cref="DomainCatalogRefreshService"/> waits on this between refreshes.</summary>
    Task WhenCatalogWakeAsync(CancellationToken cancellationToken = default);

    /// <summary>After <see cref="WhenRefinerWakeAsync"/> wins a race, drops any extra refiner tokens (burst of saves → one early run).</summary>
    void DrainExtraRefinerSignals();

    /// <summary>After <see cref="WhenCatalogWakeAsync"/> wins a race, drops extra catalog tokens.</summary>
    void DrainExtraCatalogSignals();
}
