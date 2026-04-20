using System.Text.Json.Serialization;

namespace model;

public class CloneModel: BaseModel
{
    [JsonPropertyName("cloneLog")]
    public string CloneLog { get; set; } = "";

    [JsonPropertyName("cloneServerIp")]
    public string CloneServerIp { get; set; } = "";

    protected override void InternalRefresh()
    {
    }
}
