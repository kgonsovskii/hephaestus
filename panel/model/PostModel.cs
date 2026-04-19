using System.Text.Json.Serialization;

namespace model;

public class PostModel
{
    [JsonPropertyName("_operate_isValid")] public bool IsValid { get; set; }

    [JsonPropertyName("_operate_isAtWork")]
    public bool IsAtWork { get; set; }

    [JsonPropertyName("_operation")] public string Operation { get; set; }

    [JsonPropertyName("_operate_hasToWork")]
    public bool HasToWork => ActualTime != ModifyTime;

    [JsonPropertyName("_operate_modifyTime")]
    public string ModifyTime { get; set; }

    [JsonPropertyName("_operate_actualTime")]
    public string ActualTime { get; set; }

    [JsonPropertyName("_operate_lastResult")]
    public string LastResult { get; set; }

    public string StatusLabel
    {
        get
        {
            if (IsAtWork)
            {
                if (ModifyTime != null)
                    return $" Фоновый процесс {Operation} с {ModifyTime}";
                else
                {
                    return $"Фоновый процесс {Operation}";
                }
            }
            if (ActualTime != null)
                return $"Работает с {ActualTime}";
            else
            {
                return "Работает";
            }
        }
    }
    
    public void MarkOperation(string operation)
    {
        Operation = operation;
        ModifyTime = DateTime.Now.ToString();
        IsAtWork = true;
    }
    public void MarkReady()
    {
        var dt = DateTime.Now.ToString();
        ActualTime = dt;
        ModifyTime = dt;
        IsAtWork = false;
        Operation = "";
    }
}