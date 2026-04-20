using System.Text.Json.Serialization;

namespace model;

public sealed class CloneStartBody
{
    [JsonPropertyName("cloneServerIp")]
    public string CloneServerIp { get; set; } = "";

    [JsonPropertyName("cloneUser")]
    public string CloneUser { get; set; } = "";

    [JsonPropertyName("clonePassword")]
    public string ClonePassword { get; set; } = "";
}
