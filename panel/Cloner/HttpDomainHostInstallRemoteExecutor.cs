using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cloner;

internal sealed partial class HttpDomainHostInstallRemoteExecutor : IClonerInstallExecutor
{
    public const string HttpClientName = "ClonerDomainHostInstall";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ClonerOptions> _options;
    private readonly ILogger<HttpDomainHostInstallRemoteExecutor> _logger;

    public HttpDomainHostInstallRemoteExecutor(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ClonerOptions> options,
        ILogger<HttpDomainHostInstallRemoteExecutor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RemoteInstallJob job, CancellationToken cancellationToken)
    {
        var o = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(o.DomainHostExecutorBaseUrl))
            throw new InvalidOperationException(
                "Cloner:DomainHostExecutorBaseUrl is required when Cloner:Executor is DomainHostHttp.");

        if (string.IsNullOrWhiteSpace(o.DomainHostExecutorApiKey))
            throw new InvalidOperationException(
                "Cloner:DomainHostExecutorApiKey is required when Cloner:Executor is DomainHostHttp.");

        var baseUrl = o.DomainHostExecutorBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/internal/install-remote";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation("X-Cloner-Internal-Key", o.DomainHostExecutorApiKey);
        req.Content = JsonContent.Create(new { host = job.Host, user = job.User, password = job.Password });

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"DomainHost remote install HTTP {(int)resp.StatusCode}: {err}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lastExit = 0;
        var sawExit = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
                break;

            var m = ExitLine().Match(line);
            if (m.Success && int.TryParse(m.Groups[1].ValueSpan, out var code))
            {
                sawExit = true;
                lastExit = code;
            }
            else
                await job.LogWriter.WriteAsync(line, cancellationToken).ConfigureAwait(false);
        }

        return sawExit ? lastExit : 1;
    }

    [GeneratedRegex(@"^\[exit\]\s*(-?\d+)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex ExitLine();
}
