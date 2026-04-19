using model;

namespace cp;

public class BackSvc
{
    public static Dictionary<string, string> Servers = new Dictionary<string, string >();
    private static List<string> Ips;

    public static string EvalServer(HttpRequest request) => PanelServerIdentity.DefaultKey;



}
