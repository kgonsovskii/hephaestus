using System.Text.Json.Serialization;

namespace model;

public class DomainIp
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; }
    
    [JsonPropertyName("ip")]
    public string IP { get; set; }
}