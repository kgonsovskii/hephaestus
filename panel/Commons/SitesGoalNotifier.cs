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
        Timeout = TimeSpan.FromSeconds(3)
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
            _logger.LogDebug(ex, "Sites goal skipped (server settings unavailable)");
            return;
        }

        var request = TryCreateRequest(url, ipAddress);
        if (request is null)
            return;

        _ = SendAsync(request);
    }

    internal static HttpRequestMessage? TryCreateRequest(string? sitesGoalUrl, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(sitesGoalUrl) ||
            string.IsNullOrWhiteSpace(ipAddress) ||
            string.Equals(ipAddress, "unknown", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!Uri.TryCreate(sitesGoalUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var json = JsonSerializer.Serialize(new { ip = ipAddress.Trim() });
        return new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private async Task SendAsync(HttpRequestMessage request)
    {
        try
        {
            using (request)
            using (await _http.SendAsync(request))
            {
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sites goal notify failed");
        }
    }
}
