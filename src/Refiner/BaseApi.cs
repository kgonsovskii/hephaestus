
namespace Refiner;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;


public abstract class BaseApi
{
    private readonly string _apiUrl;
    private readonly string _apiKey;

    protected BaseApi(string apiUrl, string apiKey)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
    }

    public abstract Task Process();

    protected async Task<T> PostAsync<T>(string action, object data = null)
    {
        using var client = new HttpClient();

        // Create the form data content
        var formData = new Dictionary<string, string>
        {
            { "api_key", _apiKey },
            { "action", action }
        };

      

        try
        {
            // Convert the dictionary to form-urlencoded content
            var content = new FormUrlEncodedContent(formData);

            // Post the form data to the specified URL
            var responseFlow = await client.PostAsync(_apiUrl, content);
            responseFlow.EnsureSuccessStatusCode();

            var response = (await responseFlow.Content.ReadAsStringAsync());
            return JsonSerializer.Deserialize<T>(response);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}