namespace DomainHost.Configuration;

public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    /// <summary>Directory name to find by walking up from the content root (e.g. repo <c>web\</c>).</summary>
    public string WebRoot { get; set; } = "web";

    /// <summary>How many parent directories to try when locating <see cref="WebRoot"/> (including the start folder).</summary>
    public int WebRootSearchMaxAscents { get; set; } = 10;

    public int RefreshSeconds { get; set; } = 30;

    public HttpsEndpointOptions Https { get; set; } = new();
}

public sealed class HttpsEndpointOptions
{
    public bool Enabled { get; set; }

    public string? PfxPath { get; set; }

    public string? PfxPassword { get; set; }
}
