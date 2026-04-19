using System.Globalization;
using model;

namespace Commons;

public partial class ServerService
{
    public void UpdatePack(PackItem pack)
    {
        if (string.IsNullOrEmpty(pack.Date))
            pack.Date = "Please wait...";
        pack.Validate();
    }

    public void UpdatePacks(ServerModel server)
    {
        server.Pack.Refresh();
        var packRoot = server.Pack.PackFolder;
        if (!string.IsNullOrEmpty(packRoot) && !Directory.Exists(packRoot))
            Directory.CreateDirectory(packRoot);
        foreach (var pack in server.Pack.Items)
            UpdatePack(pack);
    }

    public string PackServerRequest(ServerModel serverModel)
    {
        UpdatePacks(serverModel);
        SaveServerLite(serverModel);
        return RunExe(_loader.Paths.Packer);
    }

    public string PackServer(string packId, Action<string>? logger)
    {
        var p = GetServerLite();
        var result = RunScript(p.Server, "pack", UserPackLogPath, logger,
            new ValueTuple<string, object>("serverName", p.Server),
            new ValueTuple<string, object>("packId", packId));
        p = GetServerLite();
        foreach (var pack in p.Pack.Items)
        {
            if (string.IsNullOrEmpty(packId) || pack.Id == packId)
                pack.Date = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        SaveServerLite(p);
        return result;
    }
}
