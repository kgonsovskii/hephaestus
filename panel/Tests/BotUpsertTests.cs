using cp;
using cp.Controllers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using model;

namespace Tests;

[TestClass]
public sealed class BotUpsertTests
{
    /// <summary>Must match cp BaseController.SecretKey (<c>YourSecretKeyHere</c>).</summary>
    private const string TestSecretKey = "YourSecretKeyHere";

    private static BotController CreateBotController()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cfg = new ConfigurationBuilder().Build();
        return new BotController(null!, cfg, cache);
    }

    [TestMethod]
    public void BuildEnvelope_UnEnvelope_RoundTrips()
    {
        var req = new BotLogRequest { Id = "machine-001", Serie = "alpha", ElevatedNumber = 1 };
        var env = BotUpsertSigning.BuildEnvelope(req);

        BotUpsertSigning.ComputeEnvelopeContentHash(env.Json!).Should().Be(env.Hash);

        var ctrl = CreateBotController();
        var back = ctrl.UnEnvelope<BotLogRequest>(env);
        back.Id.Should().Be(req.Id);
        back.Serie.Should().Be(req.Serie);
        back.ElevatedNumber.Should().Be(req.ElevatedNumber);
    }

    [TestMethod]
    public void UnEnvelope_WhenInnerHashTampered_Throws()
    {
        var env = BotUpsertSigning.BuildEnvelope(new BotLogRequest { Id = "x", Serie = "y", ElevatedNumber = 0 });
        env.Hash = Convert.ToBase64String(new byte[32]);

        var ctrl = CreateBotController();
        var act = () => ctrl.UnEnvelope<BotLogRequest>(env);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Invalid hash*");
    }

    [TestMethod]
    public void XSignature_MatchesServerVerification_ForCanonicalJsonBody()
    {
        var req = new BotLogRequest { Id = "abc", Serie = "s", ElevatedNumber = 2 };
        var canonical = System.Text.Json.JsonSerializer.Serialize(req, BotUpsertSigning.UpsertJsonOptions);

        var sig = BotUpsertSigning.ComputeXSignature(canonical, TestSecretKey);
        BotUpsertSigning.VerifyXSignature(canonical, sig, TestSecretKey).Should().BeTrue();
    }

    [TestMethod]
    public void XSignature_RejectsWhitespaceChange()
    {
        var canonical =
            System.Text.Json.JsonSerializer.Serialize(
                new BotLogRequest { Id = "z", Serie = "", ElevatedNumber = 0 },
                BotUpsertSigning.UpsertJsonOptions);

        var sig = BotUpsertSigning.ComputeXSignature(canonical, TestSecretKey);
        BotUpsertSigning.VerifyXSignature(canonical + " ", sig, TestSecretKey).Should().BeFalse();
    }

    /// <summary>Optional: documents the same canonical JSON shape the Troyan bot must sign (see BotUpsertDemo.ps1).</summary>
    [TestMethod]
    public void CanonicalJson_UsesPropertyNamesLikeProduction()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new BotLogRequest { Id = "i", Serie = "ser", ElevatedNumber = 3 },
            BotUpsertSigning.UpsertJsonOptions);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"serie\":");
        json.Should().Contain("\"elevated_number\":");
    }
}
