using Domain;
using Domain.Models;

namespace DomainHost;

public interface IWebFileResolver
{
    WebFileResolution Resolve(DomainRecord record, PathString requestPath);
}

public sealed class WebFileResolver : IWebFileResolver
{
    private readonly IWebContentPathProvider _paths;
    private readonly ILogger<WebFileResolver> _logger;

    public WebFileResolver(IWebContentPathProvider paths, ILogger<WebFileResolver> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public WebFileResolution Resolve(DomainRecord record, PathString requestPath)
    {
        if (record.ContentKind == DomainContentKind.Redirect)
        {
            if (string.IsNullOrWhiteSpace(record.RedirectUrl))
            {
                _logger.LogWarning("Redirect domain {Domain} has no redirect_url.", record.Domain);
                return WebFileResolution.Missing;
            }

            return WebFileResolution.RedirectTo(record.RedirectUrl.Trim());
        }

        var relative = requestPath.Value?.TrimStart('/') ?? "";
        foreach (var root in EnumerateContentRoots(record))
        {
            if (!Directory.Exists(root))
                continue;

            if (!string.IsNullOrEmpty(relative)
                && TryMapToExistingFile(root, relative, record.ContentKind, out var specificPath, out var specificMime))
                return WebFileResolution.File(specificPath, specificMime);

            if (TryDefaultIndex(root, record.ContentKind, out var indexPath, out var indexMime))
                return WebFileResolution.File(indexPath, indexMime);
        }

        return WebFileResolution.Missing;
    }

    private IEnumerable<string> EnumerateContentRoots(DomainRecord record)
    {
        var web = _paths.WebRootFullPath;
        yield return Path.Combine(web, record.Domain);
        if (!string.IsNullOrWhiteSpace(record.DomainClass))
            yield return Path.Combine(web, record.DomainClass.Trim());
    }

    private static bool TryDefaultIndex(
        string rootFull,
        DomainContentKind kind,
        out string path,
        out string mime)
    {
        path = "";
        mime = "";
        var indexName = kind == DomainContentKind.Html ? "index.html" : "index.js";
        var candidate = Path.Combine(rootFull, indexName);
        if (!File.Exists(candidate))
            return false;
        path = candidate;
        mime = MimeFromPath(candidate, kind);
        return true;
    }

    private static bool TryMapToExistingFile(
        string rootFull,
        string relativeUrl,
        DomainContentKind kind,
        out string path,
        out string mime)
    {
        path = "";
        mime = "";
        if (!TryBuildSafePath(rootFull, relativeUrl, out var fullPath))
            return false;
        if (!File.Exists(fullPath))
            return false;
        path = fullPath;
        mime = MimeFromPath(fullPath, kind);
        return true;
    }

    private static bool TryBuildSafePath(string rootFull, string relative, out string fullPath)
    {
        fullPath = "";
        rootFull = Path.GetFullPath(rootFull);
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (p is "." or "..")
                return false;
        }

        var combined = parts.Length == 0 ? rootFull : Path.GetFullPath(Path.Combine([rootFull, ..parts]));
        if (!IsUnderRoot(rootFull, combined))
            return false;
        fullPath = combined;
        return true;
    }

    private static bool IsUnderRoot(string rootFull, string candidateFull)
    {
        rootFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootFull));
        candidateFull = Path.GetFullPath(candidateFull);
        var sep = Path.DirectorySeparatorChar;
        return candidateFull.Equals(rootFull, StringComparison.OrdinalIgnoreCase)
               || candidateFull.StartsWith(rootFull + sep, StringComparison.OrdinalIgnoreCase);
    }

    private static string MimeFromPath(string filePath, DomainContentKind fallback) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".js" or ".mjs" or ".cjs" => "application/javascript; charset=utf-8",
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".woff2" => "font/woff2",
            _ => fallback == DomainContentKind.Html
                ? "text/html; charset=utf-8"
                : "application/javascript; charset=utf-8"
        };
}
