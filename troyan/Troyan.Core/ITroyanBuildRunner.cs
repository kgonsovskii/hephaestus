using Commons;

namespace Troyan.Core;

public interface ITroyanBuildRunner
{
    void Run(string server, string packId, ServerService panelService);
}
