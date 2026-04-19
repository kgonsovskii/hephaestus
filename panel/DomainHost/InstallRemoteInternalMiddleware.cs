using System.Text.Json;
using Cloner;
using Commons;
using InstallRemote;
using Microsoft.Extensions.Options;

namespace DomainHost;

/// <summary>Runs <see cref="RemoteInstallRunner"/> on the DomainHost machine (Linux + sshpass). Secured by <see cref="DomainHostOptions.ClonerInternalApiKey"/>.</summary>
public sealed class InstallRemoteInternalMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DomainHostOptions _hostOpts;
    private readonly IOptionsMonitor<ClonerOptions> _clonerOpts;
    private readonly ILogger<InstallRemoteInternalMiddleware> _logger;

    public InstallRemoteInternalMiddleware(
        RequestDelegate next,
        IOptions<DomainHostOptions> hostOpts,
        IOptionsMonitor<ClonerOptions> clonerOpts,
        ILogger<InstallRemoteInternalMiddleware> logger)
    {
        _next = next;
        _hostOpts = hostOpts.Value;
        _clonerOpts = clonerOpts;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.Equals("/internal/install-remote", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(_hostOpts.ClonerInternalApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!string.Equals(
                context.Request.Headers["X-Cloner-Internal-Key"],
                _hostOpts.ClonerInternalApiKey,
                StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        InstallRemotePostBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<InstallRemotePostBody>(
                    context.Request.Body,
                    InstallRemoteJson.Options,
                    context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid JSON body.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Host) || string.IsNullOrWhiteSpace(body.User))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("host and user are required.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        context.Response.ContentType = "text/plain; charset=utf-8";

        try
        {
            var repoRoot = RepoRootResolver.Resolve(_clonerOpts.CurrentValue.RepoRoot, _logger);
            var scriptPath = Path.Combine(repoRoot, "install", RemoteInstallRunner.DefaultRemoteScriptFileName);
            var script = RemoteInstallRunner.LoadRemoteScriptFromFile(scriptPath);

            var sshpass = RemoteInstallRunner.FindSshPassOnPath()
                          ?? throw new InvalidOperationException(
                              "sshpass not found on PATH (e.g. apt install sshpass).");

            var code = await RemoteInstallRunner.RunRemoteInstallAsync(
                    sshpass,
                    body.Host.Trim(),
                    body.User.Trim(),
                    body.Password ?? "",
                    script,
                    async (line, ct) =>
                    {
                        await context.Response.WriteAsync(line + "\n", ct).ConfigureAwait(false);
                        await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
                    },
                    onProcessStarted: null,
                    context.RequestAborted)
                .ConfigureAwait(false);

            await context.Response.WriteAsync($"[exit] {code}\n", context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "internal install-remote failed");
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("[error] " + ex.Message + "\n", context.RequestAborted).ConfigureAwait(false);
        }
    }
}

public sealed class InstallRemotePostBody
{
    public string Host { get; set; } = "";

    public string User { get; set; } = "";

    public string? Password { get; set; }
}

internal static class InstallRemoteJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
