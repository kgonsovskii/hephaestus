using System.Globalization;

namespace model;

public partial class ServerService
{
    public void UpdatePack(PackItem pack)
    {
        if (string.IsNullOrEmpty(pack.Date))
        {
            pack.Date = "Please wait...";
        }
        pack.Validate();
    }

    public void UpdatePacks(ServerModel server)
    {
        server.Pack.Refresh();
        if (!Directory.Exists(server.Pack.PackFolder))
            Directory.CreateDirectory(server.Pack.PackFolder);
        foreach (var pack in server.Pack.Items)
        {
            UpdatePack(pack);
        }
    }
    
    public string PackServerRequest(string serverName, ServerModel serverModel)
    {
        UpdatePacks(serverModel);
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        return RunExe(ServerModelLoader.Packer, serverName);
    }

    public string PackServer(string serverName, string packId, Action<string>? logger)
    {
        var p = GetServerLite(serverName);
        var result = RunScript(p.Server, "pack",p.UserPackLog, logger,
            new ValueTuple<string, object>("serverName", p.Server),
            new ValueTuple<string, object>("packId", packId));
        p = GetServerLite(serverName);
        foreach (var pack in p.Pack.Items)
        {
            if (string.IsNullOrEmpty(packId) || pack.Id == packId)
                pack.Date = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
        SaveServerLite(serverName, p);
        return result;
    }
}