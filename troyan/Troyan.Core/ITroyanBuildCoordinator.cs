using Commons;

namespace Troyan.Core;

/// <summary>Runs the Troyan script build for the default panel server (shared by periodic refiner and web apply).</summary>
public interface ITroyanBuildCoordinator
{
    void RunDefaultServerBuild();
}
