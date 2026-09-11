using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Commons;

public sealed class SitesGoalNotifier : ISitesGoalNotifier
{
    private static readonly HttpClient SharedClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private readonly ServerService _serverService;
    private readonly ILogger<SitesGoalNotifier> _logger;
    private readonly HttpClient _http;

    public SitesGoalNotifier(ServerService serverService, ILogger<SitesGoalNotifier>? logger = null)
        : this(serverService, logger, SharedClient)
    {
    }

    internal SitesGoalNotifier(ServerService serverService, ILogger<SitesGoalNotifier>? logger, HttpClient http)
    {
        _serverService = serverService;
        _logger = logger ?? NullLogger<SitesGoalNotifier>.Instance;
        _http = http;
    }

    public void Notify(string ipAddress)
    {
        string? url;
        try
        {
            url = _serverService.GetServerLite().SitesGoalUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sites goal skipped (server settings unavailable)");
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Sites goal skipped: sitesGoalUrl is empty");
            return;
        }

        var request = TryCreateRequest(url, ipAddress);
        if (request is null)
        {
            _logger.LogWarning("Sites goal skipped: ip={Ip} url={Url}", ipAddress, url);
            return;
        }

        var ip = ClientIpNormalizer.Normalize(ipAddress);
        _ = SendAsync(request, url.Trim(), ip);
    }

    internal static HttpRequestMessage? TryCreateRequest(string? sitesGoalUrl, string? ipAddress)
    {
        var ip = ClientIpNormalizer.Normalize(ipAddress);
        if (string.IsNullOrWhiteSpace(sitesGoalUrl) || ip.Length == 0)
            return null;

        if (!Uri.TryCreate(sitesGoalUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var json = JsonSerializer.Serialize(new { ip });
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("User-Agent", "HephaestusGoal/1");
        return request;
    }

    private async Task SendAsync(HttpRequestMessage request, string url, string ip)
    {
        try
        {
            using (request)
            using (var response = await _http.SendAsync(request).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Sites goal notify {Status} {Url} ip={Ip}", (int)response.StatusCode, url, ip);
                else
                    _logger.LogWarning("Sites goal notify {Status} {Url} ip={Ip}", (int)response.StatusCode, url, ip);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sites goal notify failed {Url} ip={Ip}", url, ip);
        }
    }
}
