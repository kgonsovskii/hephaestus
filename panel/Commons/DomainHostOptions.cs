namespace Commons;

/// <summary>
/// Bound from configuration only (<see cref="SectionName"/>). Defaults live in appsettings, not here.
/// </summary>
public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    /// <summary>Relative path from the application base directory to the repository root (e.g. <c>..</c> when running from <c>output/</c>).</summary>
    public string RepositoryRoot { get; set; } = null!;

    /// <summary>Directory name beside the repository root (e.g. <c>hephaestus_data</c> → <c>../hephaestus_data</c> from the clone).</summary>
    public string HephaestusData { get; set; } = null!;

    public string WebRoot { get; set; } = null!;

    public string DomainsFileName { get; set; } = null!;

    /// <summary>File name at repository root listing domains excluded from Technitium DNS sync (e.g. <c>domains-ignore.json</c>).</summary>
    public string DomainsIgnoreFileName { get; set; } = null!;

    public string CertDirectoryName { get; set; } = null!;

    public string CertPfxFileName { get; set; } = null!;

    public string CertPublicCerFileName { get; set; } = null!;

    /// <summary>May be empty when the PFX has no password.</summary>
    public string? CertPfxPassword { get; set; }

    public int HttpPort { get; set; }

    public int HttpsPort { get; set; }

    public int StaticFileCacheMaxAgeSeconds { get; set; }

    /// <summary>Seconds to wait before retrying when the host fails to start (e.g. port busy).</summary>
    public int RetryDelaySeconds { get; set; }
}
