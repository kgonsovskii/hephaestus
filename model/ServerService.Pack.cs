using System.Diagnostics;

namespace model;

public partial class ServerService
{
    
    static string GetFileNameFromUrl(string url, string SomeBaseUri)
    {
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            uri = new Uri(new Uri(SomeBaseUri), url);

        return Path.GetFileName(uri.LocalPath);
    }
    
    public void UpdatePacks(ServerModel server)
    {
        var url = server.Alias;
        if (string.IsNullOrEmpty(url))
            url = server.ServerIp;
        url = "http://" + url + "/pack/envelope";
        server.Pack.PackTemplateUrl = url;
        server.Pack.PackRootFolder = Path.Combine(server.UserDataDir, "packs");
        foreach (var pack in server.Pack.Items)
        {
            if (string.IsNullOrEmpty(pack.Index))
                pack.Index = Guid.NewGuid().ToString();
            pack.OriginalUrl =  UrlHelper.NormalizeUrl(pack.OriginalUrl);
            pack.UrlVbs = url + "?type=vbs&url=" + pack.OriginalUrl;
            pack.UrlExe = url + "?type=exe&url=" + pack.OriginalUrl;
            pack.Name = GetFileNameFromUrl(pack.OriginalUrl, url);
            if (!pack.Name.Contains("."))
            {
                pack.Name += ".exe";
            }
            if (string.IsNullOrEmpty(pack.Date))
            {
                pack.Date = "Please wait...";
            }
            pack.PackFolder = Path.Combine(server.Pack.PackRootFolder, pack.Index);
            pack.PackFileVbs = Path.Combine(pack.PackFolder, Path.ChangeExtension(pack.Name, ".vbs"));
            pack.PackFileExe = Path.Combine(pack.PackFolder, Path.ChangeExtension(pack.Name, ".exe"));
            pack.Validate();
        }
    }
    
    public string PackServerRequest(string serverName, ServerModel serverModel)
    {
        UpdatePacks(serverModel);
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        return RunExe(ServerModelLoader.Packer, serverName);
    }

    public string PackServer(string serverName, string packId)
    {
        var p = GetServerLite(serverName);
        var result = RunScript(p.Server, "pack",p.UserPackLog,
            new ValueTuple<string, object>("serverName", p.Server),
            new ValueTuple<string, object>("packId", packId));
        p = GetServerLite(serverName);
        foreach (var pack in p.Pack.Items)
        {
            pack.Date = DateTime.Now.ToString();
        }
        SaveServerLite(serverName, p);

        return result;
    }
}