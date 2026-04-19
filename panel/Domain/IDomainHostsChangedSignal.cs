namespace Domain;

public interface IDomainHostsChangedSignal
{
        void NotifyHostsChanged();

        Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default);

        Task WhenCatalogWakeAsync(CancellationToken cancellationToken = default);

        void DrainExtraRefinerSignals();

        void DrainExtraCatalogSignals();
}
