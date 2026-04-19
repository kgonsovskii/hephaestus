namespace Domain;

public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    /// <summary>
    /// Directory name to resolve by walking parents from the process content root (sibling of the exe’s parent is typical).
    /// Default <c>data</c> (same as <c>HephaestusRepoPaths.DefaultDataDirectoryName</c>).
    /// </summary>
    public string HephaestusDataDirectoryName { get; set; } = "data";

    /// <summary>Static site folder under the resolved Hephaestus data directory. Default <c>web</c>.</summary>
    public string WebRoot { get; set; } = "web";

    public int WebRootSearchMaxAscents { get; set; } = 10;

    public string DomainsFileName { get; set; } = "domains.json";

    /// <summary>JSON file listing domain names to skip for Technitium sync (same folder as <see cref="DomainsFileName"/>, i.e. Hephaestus data root).</summary>
    public string DomainsIgnoreFileName { get; set; } = "domains-ignore.json";

    public int RefreshSeconds { get; set; } = 30;

    /// <summary>Certificate folder under the resolved Hephaestus data directory. Default <c>cert</c>.</summary>
    public string CertDirectoryName { get; set; } = "cert";

    public string CertPfxFileName { get; set; } = "hephaestus.pfx";

    public string CertPfxPassword { get; set; } = "123";

    /// <summary>HTTP listen port. Default 80.</summary>
    public int HttpPort { get; set; } = 80;

    /// <summary>HTTPS listen port. Default 443.</summary>
    public int HttpsPort { get; set; } = 443;

    /// <summary>
    /// <c>Cache-Control: public, max-age=…</c> for vhost static files and redirects under <see cref="WebRoot"/> (not <c>/cp</c>).
    /// When the web tree changes, the host bumps a revision so ETags invalidate. Default 60 seconds.
    /// </summary>
    public int StaticFileCacheMaxAgeSeconds { get; set; } = 60;
}
