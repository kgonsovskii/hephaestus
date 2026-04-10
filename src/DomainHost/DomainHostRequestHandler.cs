using System.Globalization;
using Domain;
using Microsoft.Extensions.Options;

namespace DomainHost;

public sealed class DomainHostRequestHandler
{
    private readonly IDomainCatalog _catalog;
    private readonly IWebFileResolver _fileResolver;
    private readonly WebStaticRevision _staticRevision;
    private readonly IOptionsMonitor<DomainHostOptions> _hostOptions;
    private readonly ILogger<DomainHostRequestHandler> _logger;

    public DomainHostRequestHandler(
        IDomainCatalog catalog,
        IWebFileResolver fileResolver,
        WebStaticRevision staticRevision,
        IOptionsMonitor<DomainHostOptions> hostOptions,
        ILogger<DomainHostRequestHandler> logger)
    {
        _catalog = catalog;
        _fileResolver = fileResolver;
        _staticRevision = staticRevision;
        _hostOptions = hostOptions;
        _logger = logger;
    }

    public Task<bool> TryHandleAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!_catalog.TryGetBestMatch(host, out var record))
            return Task.FromResult(false);

        var resolution = _fileResolver.Resolve(record, context.Request.Path);
        switch (resolution.Kind)
        {
            case WebFileResolutionKind.Redirect when !string.IsNullOrEmpty(resolution.RedirectLocation):
                return Task.FromResult(ServeRedirect(context, resolution.RedirectLocation));

            case WebFileResolutionKind.StaticFile when !string.IsNullOrEmpty(resolution.FilePath):
                return ServeStaticAsync(context, resolution.FilePath, resolution.ContentType!);

            default:
                _logger.LogDebug("No content for host {Host} path {Path}.", host, context.Request.Path);
                return Task.FromResult(false);
        }
    }

    private bool ServeRedirect(HttpContext context, string location)
    {
        var maxAge = GetClampedMaxAgeSeconds();
        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location = location;
        context.Response.Headers.CacheControl = maxAge > 0 ? $"public, max-age={maxAge}" : "no-store";
        return true;
    }

    private async Task<bool> ServeStaticAsync(HttpContext context, string path, string contentType)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            return false;

        var maxAge = GetClampedMaxAgeSeconds();
        var revision = _staticRevision.Current;
        var etag = MakeWeakEtag(revision, fileInfo);
        var lastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero);

        context.Response.ContentType = contentType;
        context.Response.Headers.LastModified = lastModified.ToString("R", CultureInfo.InvariantCulture);
        context.Response.Headers.ETag = etag;

        if (maxAge > 0)
            context.Response.Headers.CacheControl = $"public, max-age={maxAge}";
        else
            context.Response.Headers.CacheControl = "no-store";

        if (IsNotModified(context.Request, etag, lastModified))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentLength = fileInfo.Length;

        if (HttpMethods.IsHead(context.Request.Method))
            return true;

        await context.Response.SendFileAsync(path).ConfigureAwait(false);
        return true;
    }

    private int GetClampedMaxAgeSeconds()
    {
        var s = _hostOptions.CurrentValue.StaticFileCacheMaxAgeSeconds;
        return Math.Clamp(s, 0, 86400);
    }

    private static string MakeWeakEtag(long revision, FileInfo fileInfo) =>
        $"W/\"{revision}-{fileInfo.LastWriteTimeUtc.Ticks}-{fileInfo.Length}\"";

    private static bool IsNotModified(HttpRequest request, string etag, DateTimeOffset lastModified)
    {
        var inm = request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(inm))
        {
            foreach (var segment in inm.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (WeakEtagsEqual(segment, etag))
                    return true;
            }
        }

        var ims = request.GetTypedHeaders().IfModifiedSince;
        if (ims.HasValue && lastModified <= ims.Value.AddSeconds(1))
            return true;

        return false;
    }

    private static bool WeakEtagsEqual(string clientSegment, string serverEtag)
    {
        var a = NormalizeWeakEtag(clientSegment);
        var b = NormalizeWeakEtag(serverEtag);
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    private static string NormalizeWeakEtag(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            s = s[2..].Trim();
        return s.Trim('"');
    }
}
