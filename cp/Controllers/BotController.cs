using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("[controller]")]
public class BotController: BaseController
{
    public BotController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(serverService, configuration, memoryCache)
    {
    }
    
    [HttpGet("{profile}/{random}/{target}/DnLog")]
    public async Task<IActionResult> DnLog(string profile, string random, string target)
    {
        return await DnLog(Server, IpAddress, profile, random, target);
    }
    
    internal async Task<IActionResult> DnLog(string server, string ipAddress, string profile, string random, string target)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            await using (var command = new SqlCommand("dbo.LogDn", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@server", server ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@profile", profile ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ip", ipAddress);

                await command.ExecuteNonQueryAsync();
            }
        }

        return Ok();
    }

    [HttpPost("upsert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> UpsertBotLog([FromHeader(Name = "X-Signature")] string xSignature,
        [FromBody] EnvelopeRequest request)
    {
        var ipAddress = IpAddress;
        if (string.IsNullOrWhiteSpace(ipAddress))
            return BadRequest("IP address not found.");
        if (string.IsNullOrWhiteSpace(Server))
            return BadRequest("Server address not found.");
        return await UpsertBotLog(Server, IpAddress, xSignature, request);
    }

    internal async Task<IActionResult> UpsertBotLog(string server, string ipAddress,
        [FromHeader(Name = "X-Signature")] string xSignature, [FromBody] EnvelopeRequest request)
    {
        var realRequest = UnEnvelope<BotLogRequest>(request);
        var jsonBody = JsonSerializer.Serialize(realRequest, JsonOptions);

        if (!ValidateHash(jsonBody, xSignature, SecretKey))
        {
            return Unauthorized("Invalid signature.");
        }

        try
        {
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                await using (var command = new SqlCommand("dbo.UpsertBotLog", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@server", server ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ip", ipAddress);
                    command.Parameters.AddWithValue("@id", realRequest.Id);
                    command.Parameters.AddWithValue("@elevated", realRequest.ElevatedNumber);
                    command.Parameters.AddWithValue("@serie", realRequest.Serie ?? (object)DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return Ok("{}");
        }
        catch (Exception ex)
        {
            // Log the exception (ex) here
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private static bool ValidateHash(string data, string hash, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            // Debugging: Print the computed hash
            Console.WriteLine($"Computed hash on server: {computedHash}");
            return computedHash.Equals(hash);
        }
    }
    
    [HttpGet("update")]
    public IActionResult Update()
    {
        return Update(Server);
    }
    
    internal IActionResult Update(string server)
    {
        var fileContent = System.IO.File.ReadAllText(ServerModelLoader.UserDataBody(server));
        var fileBytes = EnvelopeToBytes(fileContent);
        return File(fileBytes, "text/plain");
    }
    
    public string Envelope(string dataString)
    {
        var hash = ComputeSHA256Hash(dataString);
        var payload = new { json = dataString, hash = hash };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload,JsonOptions);
        return jsonPayload;
    }
    
    public T UnEnvelope<T>(EnvelopeRequest envelopeRequest) where T: new()
    {
        var hash = ComputeSHA256Hash(envelopeRequest.Json);
        if (hash != envelopeRequest.Hash)
        {
            throw new InvalidOperationException("Invalid hash.");
        }
        var real = System.Text.Json.JsonSerializer.Deserialize<T>(envelopeRequest.Json,JsonOptions);
        return real!;
    }
    
    public byte[] EnvelopeToBytes(string dataString)
    {
        var data = Envelope(dataString);
        var bytes = Encoding.UTF8.GetBytes(data);
        return bytes;
    }
    
    private static string ComputeSHA256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}