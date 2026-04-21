using System.Text;
using System.Text.Json;
using Commons;
using cp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;
using Npgsql;

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
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            await using (var command = new NpgsqlCommand("SELECT log_dn(@server, @profile, @ip)", connection))
            {
                command.Parameters.AddWithValue("server", (object?)server ?? DBNull.Value);
                command.Parameters.AddWithValue("profile", (object?)profile ?? DBNull.Value);
                command.Parameters.AddWithValue("ip", ipAddress);

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

        if (!BotUpsertSigning.VerifyXSignature(jsonBody, xSignature, SecretKey))
        {
            return Unauthorized("Invalid signature.");
        }

        try
        {
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                await using (var command = new NpgsqlCommand(
                                   "SELECT upsert_bot_log(@server, @ip, @id, @elevated, @serie, @time_dif)", connection))
                {
                    command.Parameters.AddWithValue("server", (object?)server ?? DBNull.Value);
                    command.Parameters.AddWithValue("ip", ipAddress);
                    command.Parameters.AddWithValue("id", realRequest.Id);
                    command.Parameters.AddWithValue("elevated", realRequest.ElevatedNumber);
                    command.Parameters.AddWithValue("serie", (object?)realRequest.Serie ?? DBNull.Value);
                    command.Parameters.AddWithValue("time_dif", 0);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return Ok("{}");
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("update")]
    public IActionResult Update()
    {
        return Update(Server);
    }
    
    internal IActionResult Update(string server)
    {
        var fileContent = System.IO.File.ReadAllText(_serverService.Paths.UserDataBody);
        var fileBytes = EnvelopeToBytes(fileContent);
        return File(fileBytes, "text/plain");
    }
    
    public string Envelope(string dataString)
    {
        var hash = BotUpsertSigning.ComputeEnvelopeContentHash(dataString);
        var payload = new { json = dataString, hash = hash };
        var jsonPayload = JsonSerializer.Serialize(payload,JsonOptions);
        return jsonPayload;
    }
    
    public T UnEnvelope<T>(EnvelopeRequest envelopeRequest) where T: new()
    {
        var hash = BotUpsertSigning.ComputeEnvelopeContentHash(envelopeRequest.Json);
        if (hash != envelopeRequest.Hash)
        {
            throw new InvalidOperationException("Invalid hash.");
        }
        var real = JsonSerializer.Deserialize<T>(envelopeRequest.Json,JsonOptions);
        return real!;
    }
    
    public byte[] EnvelopeToBytes(string dataString)
    {
        var data = Envelope(dataString);
        var bytes = Encoding.UTF8.GetBytes(data);
        return bytes;
    }
    
}
