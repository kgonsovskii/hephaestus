using System.Text.Json.Serialization;

namespace model;

public class DnSponsorModel
{
    public ServerModel _server;
        
    public DnSponsorModel(ServerModel serverModel)
    {
        _server = serverModel;
    }
        
    public DnSponsorModel()
    {
           
    }

    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
        
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("url")] public string Url { get; set; } = "";
}
