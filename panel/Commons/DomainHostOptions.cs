namespace Commons;

/// <summary>
/// Bound from configuration only (<see cref="SectionName"/>). Defaults live in appsettings, not here.
/// </summary>
public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    public string HephaestusDataDirectoryName { get; set; } = null!;

    public string WebRoot { get; set; } = null!;

    public int WebRootSearchMaxAscents { get; set; }

    public string DomainsFileName { get; set; } = null!;

    public string DomainsIgnoreFileName { get; set; } = null!;

    public int RefreshSeconds { get; set; }

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

    /// <summary>When non-empty, <c>POST /internal/install-remote</c> requires header <c>X-Cloner-Internal-Key</c> to match. When empty, the endpoint is open (same-machine happy path).</summary>
    public string? ClonerInternalApiKey { get; set; }

    /// <summary>Optional subdirectory under the resolved Hephaestus data root for per-server panel folders. Empty = use data root.</summary>
    public string PanelServersSubdirectory { get; set; } = "";

    /// <summary>If set, absolute root for per-server panel data (overrides data root + <see cref="PanelServersSubdirectory"/>).</summary>
    public string? PanelServersRootOverride { get; set; }
}
