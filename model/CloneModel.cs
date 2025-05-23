using System.Text.Json.Serialization;

namespace model;

public class CloneModel: BaseModel
{
    [JsonPropertyName("cloneLog")]
    public string CloneLog { get; set; } = "";
    
    [JsonPropertyName("cloneServerIp")]
    public string CloneServerIp { get; set; } = "";
    
    [JsonPropertyName("cloneUser")]
    public string CloneUser { get; set; } = "";
    
    [JsonPropertyName("clonePassword")]
    public string ClonePassword { get; set; } = "";
    
    protected override void InternalRefresh()
    {
    }
}