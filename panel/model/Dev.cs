using System.Net;
using System.Text.Json;

namespace model;

public class Dev
{
    public static string Mode
    {
        get
        {
#if DEBUG
            return ModeDebug ;
#else
            return ModeDefault;
#endif
        }
    }

    public const string ModeDefault = PanelServerIdentity.DefaultKey;
    public const string ModeDebug = PanelServerIdentity.DefaultKey;

    public static string[] KnownDomains = new[] { "masterhost.online", "masterhost2.online" };
    public static string[] KnownSubs = new[] {"", "cp", "dev" };

    private static string DevIp = "127.0.0.1";
    private static string DevHost = "localhost.masterhost.online:5000";
    private static string DevPassword = "Putin123";

    /// <summary>Creates the default panel home and <c>server.json</c> when missing.</summary>
    public static void EnsureDefaultPanel(IPanelServerPaths paths, JsonSerializerOptions jso, string? serverIp = null)
    {
        if (!Directory.Exists(paths.RootData))
            Directory.CreateDirectory(paths.RootData);
        if (!Directory.Exists(paths.ServerDir))
            Directory.CreateDirectory(paths.ServerDir);
        if (File.Exists(paths.DataFile))
            return;
        var server = new ServerModel { Server = PanelServerIdentity.DefaultKey };

        if (!string.IsNullOrEmpty(serverIp))
            server.ServerIp = serverIp;

        ServerNetworkRefinement.FillIfUnset(server);

        File.WriteAllText(paths.DataFile, JsonSerializer.Serialize(server, jso));
    }

    public static bool IsPrivateIP(IPAddress ipAddress)
    {
        if (ipAddress.ToString() == "127.0.0.1")
            return true;
        if (ipAddress.ToString().StartsWith("169."))
            return true;
        byte[] bytes = ipAddress.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            _ => false,
        };
    }
}
