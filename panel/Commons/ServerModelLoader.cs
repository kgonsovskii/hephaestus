using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using model;

namespace Commons;

/// <summary>Factory for <see cref="ServerModel"/> JSON persistence. Does not attach runtime path state to models.</summary>
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

    public ServerModel Load()
    {
        try
        {
            return LoadFile(_paths.DataFile);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Default panel bootstrap");
            Dev.EnsureDefaultPanel(_paths, Jso);
            return LoadFile(_paths.DataFile);
        }
    }

    public ServerModel LoadFile(string serverFile)
    {
        var server = LoadFileInternal(serverFile);
        server.Refresh();
        return server;
    }

    public ServerModel LoadFileInternal(string serverFile)
    {
        var server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(serverFile), Jso)!;
        return server;
    }

    public void SaveFile(string serverFile, ServerModel server)
    {
        File.WriteAllText(serverFile, JsonSerializer.Serialize(server, Jso));
    }

    public void Save(ServerModel server) => SaveFile(_paths.DataFile, server);
}
