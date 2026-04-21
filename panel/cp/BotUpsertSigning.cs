using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using model;

namespace cp;

/// <summary>Cryptographic helpers for <c>POST /bot/upsert</c>: inner SHA-256 envelope hash and HMAC-SHA256 <c>X-Signature</c>.</summary>
public static class BotUpsertSigning
{
    /// <summary>Same options as <see cref="Controllers.BaseController"/> JSON for bot payloads.</summary>
    public static readonly JsonSerializerOptions UpsertJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string ComputeEnvelopeContentHash(string jsonUtf8)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonUtf8));
        return Convert.ToBase64String(hashBytes);
    }

    public static string ComputeXSignature(string canonicalBotLogJsonUtf8, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalBotLogJsonUtf8)));
    }

    public static bool VerifyXSignature(string canonicalBotLogJsonUtf8, string xSignatureBase64, string secretKey)
    {
        return ComputeXSignature(canonicalBotLogJsonUtf8, secretKey).Equals(xSignatureBase64);
    }

    public static EnvelopeRequest BuildEnvelope(BotLogRequest request)
    {
        var json = JsonSerializer.Serialize(request, UpsertJsonOptions);
        return new EnvelopeRequest
        {
            Json = json,
            Hash = ComputeEnvelopeContentHash(json)
        };
    }
}
