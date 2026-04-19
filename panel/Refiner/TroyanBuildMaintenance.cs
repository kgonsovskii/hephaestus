using Commons;
using Microsoft.Extensions.Logging;
using Troyan.Core;

namespace Refiner;

public sealed class TroyanBuildMaintenance : ITroyanBuildMaintenance
{
    private readonly ITroyanBuildCoordinator _coordinator;
    private readonly ILogger<TroyanBuildMaintenance> _logger;

    public TroyanBuildMaintenance(ITroyanBuildCoordinator coordinator, ILogger<TroyanBuildMaintenance> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("Troyan periodic build starting.");
                try
                {
                    _coordinator.RunDefaultServerBuild();
                    _logger.LogInformation("Troyan periodic build finished.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Troyan periodic build failed.");
                }
            },
            cancellationToken);
    }
}
