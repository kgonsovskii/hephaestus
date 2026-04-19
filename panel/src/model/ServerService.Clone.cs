namespace model;

public partial class ServerService
{
    public string CloneServerRequest(string serverName, ServerModel serverModel)
    {
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        var result = RunScript(p.Server, "install-request", p.UserCloneLog, (a) => Console.WriteLine(a),
            new ValueTuple<string, object>("serverName", p.Server));
        return result;
    }
}