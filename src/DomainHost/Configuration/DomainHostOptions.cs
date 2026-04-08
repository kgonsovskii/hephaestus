namespace DomainHost.Configuration;

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

    public int HttpsPort { get; set; } = 5443;
}
