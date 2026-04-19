using System.Text.Json.Serialization;

namespace model;

public class DnSponsorModel: BaseModel
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
        
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("url")] public string Url { get; set; } = "";
    
    protected override void InternalRefresh()
    {
    }
}
