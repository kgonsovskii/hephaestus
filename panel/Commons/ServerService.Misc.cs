using model;

namespace Commons;

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
        server.Tabs = [new TabModel(server) { Id = "default" }];
    }
}
