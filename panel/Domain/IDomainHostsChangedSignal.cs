namespace Domain;

public interface IDomainHostsChangedSignal
{
        void NotifyHostsChanged();

        Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default);

        Task WhenCatalogWakeAsync(CancellationToken cancellationToken = default);

        Task WhenTroyanWakeAsync(CancellationToken cancellationToken = default);

        Task WhenLandingWakeAsync(CancellationToken cancellationToken = default);

        void DrainExtraRefinerSignals();

        void DrainExtraCatalogSignals();

        void DrainExtraTroyanSignals();

        void DrainExtraLandingSignals();
}
