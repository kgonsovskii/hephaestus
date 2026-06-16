using model;

namespace Commons;

public partial class ServerService
{
    /// <summary>Refreshes derived server data (tabs, packs, …).</summary>
    public void RefineAndCommons(ServerModel server)
    {
        ServerCommons(server);
    }

    /// <summary>Full server record refresh used after control-panel or domain apply: refine, commons, persist.</summary>
    public void RefineCommonsAndSave(ServerModel server)
    {
        RefineAndCommons(server);
        SaveServerLite(server);
    }
}
