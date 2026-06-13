using Commons;
using Microsoft.Extensions.Logging;

namespace Git;

public sealed class HephaestusDataGitMaintenance : IHephaestusDataGitMaintenance
{
    private readonly IHephaestusPathResolver _paths;
    private readonly ILogger<HephaestusDataGitMaintenance> _logger;

    public HephaestusDataGitMaintenance(IHephaestusPathResolver paths, ILogger<HephaestusDataGitMaintenance> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    HephaestusDataGitRunner.Run(_paths, _logger, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogErrorMessage(ex, "Hephaestus data git maintenance failed.");
                }
            },
            cancellationToken);
    }
}
