namespace DomainHost.Services;

public sealed class DomainHostRequestHandler
{
    private readonly IDomainCatalog _catalog;
    private readonly IWebFileResolver _fileResolver;
    private readonly ILogger<DomainHostRequestHandler> _logger;

    public DomainHostRequestHandler(
        IDomainCatalog catalog,
        IWebFileResolver fileResolver,
        ILogger<DomainHostRequestHandler> logger)
    {
        _catalog = catalog;
        _fileResolver = fileResolver;
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
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = resolution.RedirectLocation;
                context.Response.Headers.CacheControl = "public, max-age=60";
                return Task.FromResult(true);

            case WebFileResolutionKind.StaticFile when !string.IsNullOrEmpty(resolution.FilePath):
                return ServeStaticAsync(context, resolution.FilePath, resolution.ContentType!);

            default:
                _logger.LogDebug("No content for host {Host} path {Path}.", host, context.Request.Path);
                return Task.FromResult(false);
        }
    }

    private static async Task<bool> ServeStaticAsync(HttpContext context, string path, string contentType)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            return false;

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.Headers.CacheControl = "public, max-age=60";
        context.Response.ContentLength = fileInfo.Length;

        if (HttpMethods.IsHead(context.Request.Method))
            return true;

        await context.Response.SendFileAsync(path).ConfigureAwait(false);
        return true;
    }
}
