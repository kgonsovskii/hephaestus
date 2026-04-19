using System.Text.Json.Serialization;

namespace model;

    public class BuxModel: BaseModel
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
        
        [JsonPropertyName("id")] public string Id { get; set; } = "";

        [JsonPropertyName("apiKey")] public string ApiKey { get; set; } = "";

        [JsonPropertyName("apiUrl")] public string ApiUrl { get; set; } = "";
        
        protected override void InternalRefresh()
        {
        }
    }
