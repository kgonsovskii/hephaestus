using System.Net.Http;
using Commons;
using FluentAssertions;

namespace Tests;

[TestClass]
public sealed class SitesGoalNotifierTests
{
    [TestMethod]
    public void TryCreateRequest_EmptyUrl_ReturnsNull()
    {
        SitesGoalNotifier.TryCreateRequest("", "1.2.3.4").Should().BeNull();
        SitesGoalNotifier.TryCreateRequest("http://4tube.xyz/internal/track/goal", "").Should().BeNull();
        SitesGoalNotifier.TryCreateRequest("http://4tube.xyz/internal/track/goal", "unknown").Should().BeNull();
    }

    [TestMethod]
    public void TryCreateRequest_PostsJsonIp()
    {
        using var request = SitesGoalNotifier.TryCreateRequest("http://4tube.xyz/internal/track/goal", "192.168.1.50");

        request.Should().NotBeNull();
        request!.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.ToString().Should().Be("http://4tube.xyz/internal/track/goal");
        request.Content.Should().NotBeNull();
        var json = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        json.Should().Contain("\"ip\":\"192.168.1.50\"");
    }

    [TestMethod]
    public void TryCreateRequest_NormalizesIpv4Mapped()
    {
        using var request = SitesGoalNotifier.TryCreateRequest("http://4tube.xyz/internal/track/goal", "::ffff:192.168.1.50");

        request.Should().NotBeNull();
        var json = request!.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        json.Should().Contain("\"ip\":\"192.168.1.50\"");
    }

    [TestMethod]
    public void ServerModel_DeserializesSitesGoalUrl()
    {
        var model = System.Text.Json.JsonSerializer.Deserialize<model.ServerModel>(
            "{\"sitesGoalUrl\":\"http://4tube.xyz/internal/track/goal\"}");

        model.Should().NotBeNull();
        model!.SitesGoalUrl.Should().Be("http://4tube.xyz/internal/track/goal");
    }
}
