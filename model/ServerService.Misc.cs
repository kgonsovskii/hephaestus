using System.Diagnostics;

namespace model;

public partial class ServerService
{
    public void UpdateBux(ServerModel server)
    {
        if (server.Bux == null)
            server.Bux = new List<BuxModel>();
        if (server.Bux.FirstOrDefault(a => a.Id == "unu.im") == null)
            server.Bux.Add(new BuxModel() { Id = "unu.im" });
    }

    public void UpdateDnSponsor(ServerModel server)
    {
        if (server.DnSponsor == null)
            server.DnSponsor = new List<DnSponsorModel>();
        if (server.DnSponsor.FirstOrDefault(a => a.Id == "ufiler.biz") == null)
            server.DnSponsor.Add(new DnSponsorModel() { Id = "ufiler.biz" });
    }
    
    public void UpdateTabs(ServerModel server)
    {
        var profilesDir = Path.Combine(server.UserDataDir, "profiles");
        if (System.IO.Directory.Exists(profilesDir) == false)
        {
            System.IO.Directory.CreateDirectory(profilesDir);
        }

        var profs = System.IO.Directory.GetDirectories(profilesDir);
        var result = new List<TabModel>();
        foreach (var profile in profs)
        {
            var tab = new TabModel(server);
            tab.Id = System.IO.Path.GetFileName(profile);
            tab._server = server;
            result.Add(tab);
        }

        if (result.Count == 0)
        {
            result.Add(new TabModel(server) { Id = "default" });
        }

        server.Tabs = result;
    }
}