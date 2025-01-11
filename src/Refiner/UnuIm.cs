using System.Text.Json.Serialization;
using model;

namespace Refiner;

public class ApiResponse
{
    [JsonConverter(typeof(BooleanStringConverter))]
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public string Errors { get; set; }
}

public class BalanceReponse : ApiResponse
{
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("freeze")]
    public decimal Freeze { get; set; }
}

public class UnuIm: BaseApi
{
    public UnuIm(BuxModel model) : base(model.ApiUrl, model.ApiKey)
    {
    }

    public override async Task Process()
    {
        Console.WriteLine(await GetBalance());
    }

    protected async Task<decimal> GetBalance()
    {
        var result = await PostAsync<BalanceReponse>("get_balance");
        return result.Balance;
    }
}