using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using model;

namespace Commons;

public sealed class ServerModelLoader
{
    public const string BodyFileConst = "body.txt";

    public JsonSerializerOptions Jso { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IPanelServerPaths _paths;
    private readonly ILogger<ServerModelLoader>? _logger;

    public ServerModelLoader(IPanelServerPaths paths, ILogger<ServerModelLoader>? logger = null)
    {
        _paths = paths;
        _logger = logger;
    }

    public IPanelServerPaths Paths => _paths;

    public ServerModel LoadServer(string serverName)
    {
        try
        {
            return LoadServerFile(_paths.DataFile(serverName));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Default server bootstrap for {Server}", serverName);
            Dev.DefaultServer(serverName, _paths, Jso);
            return LoadServerFile(_paths.DataFile(serverName));
        }
    }

    public ServerModel LoadServerFile(string serverFile)
    {
        var server = LoadServerFileInternal(serverFile);
        server.Refresh();
        AttachPaths(server);
        return server;
    }

    public ServerModel LoadServerFileInternal(string serverFile)
    {
        var server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(serverFile), Jso)!;
        AttachPaths(server);
        return server;
    }

    public void SaveServerFile(string serverFile, ServerModel server)
    {
        AttachPaths(server);
        File.WriteAllText(serverFile, JsonSerializer.Serialize(server, Jso));
    }

    public void SaveServer(string serverName, ServerModel server)
    {
        SaveServerFile(_paths.DataFile(serverName), server);
    }

    private void AttachPaths(ServerModel server) => server.Paths = _paths;
}
