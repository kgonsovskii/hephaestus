namespace DomainTool.Configuration;

public sealed class DomainToolOptions
{
    public const string SectionName = "DomainTool";

    public string HostsPath { get; set; } = @"C:\Windows\System32\drivers\etc\hosts";

    public string WebRoot { get; set; } = "web";

    public int WebRootSearchMaxAscents { get; set; } = 10;

    public string DomainsFileName { get; set; } = "domains.json";
}
