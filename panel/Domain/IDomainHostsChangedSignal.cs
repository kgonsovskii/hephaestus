namespace Domain;

public interface IDomainHostsChangedSignal
{
        void NotifyHostsChanged();

        Task WhenRefinerWakeAsync(CancellationToken cancellationToken = default);

        Task WhenTroyanWakeAsync(CancellationToken cancellationToken = default);

        Task WhenHephaestusDataWakeAsync(CancellationToken cancellationToken = default);

        void DrainExtraRefinerSignals();

        void DrainExtraTroyanSignals();

        void DrainExtraHephaestusDataSignals();
}
