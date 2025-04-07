using System.Text.Json.Serialization;

namespace model;

public class PackModel
{
    [JsonPropertyName("packLog")]
    public string PackLog { get; set; } = string.Empty;

    [JsonPropertyName("items")] public List<PackItem> Items { get; set; } = new List<PackItem>() { };
}

public class PackItem
{
    [JsonPropertyName("index")] public string Index { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    
    [JsonPropertyName("originalUrl")] public string OriginalUrl { get; set; } = "";
    
    [JsonPropertyName("url")] public string Url { get; set; } = "";

    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("date")] public string Date { get; set; } = "";
}
