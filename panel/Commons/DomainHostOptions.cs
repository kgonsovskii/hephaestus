namespace Commons;

/// <summary>
/// Bound from configuration only (<see cref="SectionName"/>). Defaults live in appsettings, not here.
/// </summary>
public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    /// <summary>Relative path from repository root to the Hephaestus data directory (e.g. <c>../hephaestus_data</c>).</summary>
    public string HephaestusData { get; set; } = null!;

    public string WebRoot { get; set; } = null!;

    public int WebRootSearchMaxAscents { get; set; }

    public string DomainsFileName { get; set; } = null!;

    public string DomainsIgnoreFileName { get; set; } = null!;

    public string CertDirectoryName { get; set; } = null!;

    public string CertPfxFileName { get; set; } = null!;

    public string CertPublicCerFileName { get; set; } = null!;

    /// <summary>May be empty when the PFX has no password.</summary>
    public string? CertPfxPassword { get; set; }

    public int HttpPort { get; set; }

    public int HttpsPort { get; set; }

    public int StaticFileCacheMaxAgeSeconds { get; set; }

    public string RepositoryMarkerFileName { get; set; } = null!;

    public int RepositoryRootSearchMaxAscents { get; set; }

    /// <summary>Seconds to wait before retrying when the host fails to start (e.g. port busy).</summary>
    public int RetryDelaySeconds { get; set; }
}
