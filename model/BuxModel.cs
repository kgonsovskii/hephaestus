using System.Text.Json.Serialization;

namespace model;

    public class BuxModel
    {
        public ServerModel _server;
        
        public BuxModel(ServerModel serverModel)
        {
            _server = serverModel;
        }
        
        public BuxModel()
        {
           
        }

        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
        
        [JsonPropertyName("id")] public string Id { get; set; } = "";

        [JsonPropertyName("apiKey")] public string ApiKey { get; set; } = "";

        [JsonPropertyName("apiUrl")] public string ApiUrl { get; set; } = "";
    }
