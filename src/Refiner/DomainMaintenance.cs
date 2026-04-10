namespace Refiner;

public interface IDomainMaintenance : IMaintenance
{
}

/// <summary>Placeholder for future domain-related periodic work. Currently a no-op.</summary>
public sealed class DomainMaintenance : IDomainMaintenance
{
    public Task RunAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
