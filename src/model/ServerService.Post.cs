namespace model;

public partial class ServerService
{

    public string PostServerRequest(string serverName, ServerModel serverModel, string action)
    {
        if (!Directory.Exists(ServerDir(serverName)))
            return $"Server {serverName} is not registered";

        ServerCommons(serverName, serverModel);

        var p = GetServerLite(serverName);
        if (p.PostModel.Operation != "apply" && action == "apply")
        {
            p.PostModel.Operation = "apply";
        }
        else
        {
            p.PostModel.Operation = action;
        }
        serverModel.PostModel.MarkOperation(p.PostModel.Operation);
        SaveServerLite(serverName, serverModel);
        return RunExe(ServerModelLoader.Refiner, serverName, action);
    }

    public string PostServerAction(string serverName, ServerModel serverModel, Action<string> logger)
    {
        if (string.IsNullOrEmpty(serverModel.PostModel.Operation))
            serverModel.PostModel.Operation = "exe";
        ServerCommons(serverName, serverModel);
        
        var result = RunScript(serverModel.Server, "compile", serverModel.UserPostLog, logger,
            new ValueTuple<string, object>("serverName", serverModel.Server),
            new ValueTuple<string, object>("action", serverModel.PostModel.Operation));
        serverModel.PostModel.LastResult = result;
        serverModel.PostModel.MarkReady();
        SaveServerLite(serverName, serverModel);
        return serverModel.PostModel.LastResult;
    }
}