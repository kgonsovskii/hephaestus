namespace model;

public partial class ServerService
{
    public string CloneServerRequest(string serverName, ServerModel serverModel)
    {
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        return RunExe(ServerModelLoader.Cloner, serverName);
    }

    public string CloneServer(string serverName, Action<string> logger)
    {
        var p = GetServerLite(serverName);
        Console.WriteLine(p.Server +":" + p.ServerIp + "/" + p.Alias + ">> " + p.CloneModel.CloneServerIp);
        var result = RunScript(p.Server, "install", p.UserCloneLog, logger,
            new ValueTuple<string, object>("serverName", p.Server));

        return result;
    }

}