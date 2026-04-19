namespace Commons;

public interface IMaintenance
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
