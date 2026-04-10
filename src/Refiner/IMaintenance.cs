namespace Refiner;

public interface IMaintenance
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
