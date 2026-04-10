namespace Domain;

public sealed class DomainHostOptions
{
    public const string SectionName = "DomainHost";

    public string WebRoot { get; set; } = "web";

    public int WebRootSearchMaxAscents { get; set; } = 10;

    public string DomainsFileName { get; set; } = "domains.json";

    public int RefreshSeconds { get; set; } = 30;

    public string CertDirectoryName { get; set; } = "cert";

    public string CertPfxFileName { get; set; } = "hephaestus.pfx";

    public string CertPfxPassword { get; set; } = "123";

    /// <summary>HTTP listen port. Default 80.</summary>
    public int HttpPort { get; set; } = 80;

    /// <summary>HTTPS listen port. Default 443.</summary>
    public int HttpsPort { get; set; } = 443;
}
