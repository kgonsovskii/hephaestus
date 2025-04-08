using System.Diagnostics;

namespace model;

public partial class ServerService
{
    public string CloneServerRequest(string serverName, ServerModel serverModel)
    {
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        return RunExe(ServerModelLoader.Cloner, serverName);
    }

    public string CloneServer(string serverName)
    {
        var p = GetServerLite(serverName);
        var result = RunScript(p.Server, "install", p.UserCloneLog,
            new ValueTuple<string, object>("serverName", p.Server));

        return result;
    }

}