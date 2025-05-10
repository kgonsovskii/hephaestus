namespace model;

public partial class ServerService
{
    public string CloneServerRequest(string serverName, ServerModel serverModel)
    {
        SaveServerLite(serverName, serverModel);
        var p = GetServerLite(serverName);
        return RunExe(ServerModelLoader.Cloner, serverName);
    }

}