using Commons;
using model;

namespace Troyan.Core;

public sealed class TroyanBuildCoordinator : ITroyanBuildCoordinator
{
    private readonly ITroyanBuildRunner _runner;
    private readonly ServerService _serverService;

    public TroyanBuildCoordinator(ITroyanBuildRunner runner, ServerService serverService)
    {
        _runner = runner;
        _serverService = serverService;
    }

    public void RunDefaultServerBuild() =>
        _runner.Run(PanelServerIdentity.DefaultKey, "", _serverService);
}
