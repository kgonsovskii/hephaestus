using System.Text.Json.Serialization;

namespace model;

public class DomainIp
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("ip")]
    public string IP { get; set; }

    [JsonPropertyName("domains")]
    public List<string> Domains { get; set; } = new List<string>();
    
    [JsonPropertyName("ftp")]
    public string Ftp  { get; set; }
    
    [JsonIgnore] public string? Result { get; set; }
}