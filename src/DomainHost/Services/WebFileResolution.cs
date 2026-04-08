namespace DomainHost.Services;

public enum WebFileResolutionKind
{
    None,
    StaticFile,
    Redirect
}

public readonly struct WebFileResolution
{
    public WebFileResolutionKind Kind { get; init; }

    public string? FilePath { get; init; }

    public string? ContentType { get; init; }

    public string? RedirectLocation { get; init; }

    public static WebFileResolution Missing => new() { Kind = WebFileResolutionKind.None };

    public static WebFileResolution File(string path, string contentType) =>
        new()
        {
            Kind = WebFileResolutionKind.StaticFile,
            FilePath = path,
            ContentType = contentType
        };

    public static WebFileResolution RedirectTo(string location) =>
        new()
        {
            Kind = WebFileResolutionKind.Redirect,
            RedirectLocation = location
        };
}
