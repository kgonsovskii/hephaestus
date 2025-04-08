using System.Text.Json.Serialization;

namespace model;

public class PackModel
{
    [JsonPropertyName("packRootFolder")]
    public string PackRootFolder{ get; set; } = string.Empty;
    
    [JsonPropertyName("packTemplateUrl")]
    public string PackTemplateUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("packLog")]
    public string PackLog { get; set; } = string.Empty;

    [JsonPropertyName("items")] public List<PackItem> Items { get; set; } = new List<PackItem>() { };
}

public class PackItem
{
    [JsonPropertyName("packFolder")]
    public string PackFolder{ get; set; } = string.Empty;
    
    [JsonPropertyName("packFileVbs")]
    public string PackFileVbs{ get; set; } = string.Empty;
    
    [JsonPropertyName("packFileExe")]
    public string PackFileExe{ get; set; } = string.Empty;

    
    [JsonPropertyName("index")] public string Index { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    
    [JsonPropertyName("originalUrl")] public string OriginalUrl { get; set; } = "";
    
    [JsonPropertyName("urlExe")] public string UrlExe { get; set; } = "";
    [JsonPropertyName("urlVbs")] public string UrlVbs { get; set; } = "";

    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("date")] public string Date { get; set; } = "";

    public void Validate()
    {
        if (!System.IO.File.Exists(PackFileExe) || !System.IO.File.Exists(PackFileVbs))
        {
            if (!Directory.Exists(PackFolder))
                Directory.CreateDirectory(PackFolder);
        }
    }
}
