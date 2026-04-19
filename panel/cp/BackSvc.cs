using model;

namespace cp;

public class BackSvc
{
    public static Dictionary<string, string> Servers = new Dictionary<string, string >();
    private static List<string> Ips;

    public static string EvalServer(HttpRequest request)
    {
        if (request.Headers.TryGetValue("HTTP_X_SERVER", out Microsoft.Extensions.Primitives.StringValues value))
        {
            var serverFor = value.First();
            var server = serverFor.Split(',').Select(s => s.Trim()).FirstOrDefault().Trim();
            return server;
        }
        return Dev.Mode;
    }



}
