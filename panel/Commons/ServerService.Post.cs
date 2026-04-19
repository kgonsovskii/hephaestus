using model;

namespace Commons;

public partial class ServerService
{
    public string PostServerRequest(ServerModel serverModel, string action)
    {
        if (!Directory.Exists(Paths.UserDataDir))
            return "Panel server home is not registered";

        ServerCommons(serverModel);

        var p = GetServerLite();
        if (p.PostModel.Operation != "apply" && action == "apply")
            p.PostModel.Operation = "apply";
        else
            p.PostModel.Operation = action;
        serverModel.PostModel.MarkOperation(p.PostModel.Operation);
        SaveServerLite(serverModel);
        return "OK";
    }

    public string PostServerAction(ServerModel serverModel, Action<string> logger)
    {
        if (string.IsNullOrEmpty(serverModel.PostModel.Operation))
            serverModel.PostModel.Operation = "exe";
        ServerCommons(serverModel);

        var result = RunScript(serverModel.Server, "compile", UserPostLogPath, logger,
            new ValueTuple<string, object>("serverName", serverModel.Server),
            new ValueTuple<string, object>("action", serverModel.PostModel.Operation));
        serverModel.PostModel.LastResult = result;
        serverModel.PostModel.MarkReady();
        SaveServerLite(serverModel);
        return serverModel.PostModel.LastResult;
    }
}
