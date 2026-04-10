namespace Domain;

/// <summary>
/// Lets callers request an early run of hosted work that reacts to <c>domains.json</c> changes
/// (<see cref="Refiner.RefinerBackgroundService"/> domain maintenance, <see cref="DomainCatalogRefreshService"/>).
/// Does not run work inline — only wakes waiters so the next scheduled loop iteration runs soon.
/// </summary>
public interface IDomainHostsChangedSignal
{
    /// <summary>Wake any waiter (Refiner domain loop, catalog refresh) so they do not sleep the full interval.</summary>
    void NotifyHostsChanged();

    /// <summary>Completes when <see cref="NotifyHostsChanged"/> is called; multiple waiters on the same generation share one completion.</summary>
    Task WhenHostsChangedAsync(CancellationToken cancellationToken = default);
}
