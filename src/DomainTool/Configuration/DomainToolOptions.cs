namespace DomainTool.Configuration;

public sealed class DomainToolOptions
{
    public const string SectionName = "DomainTool";

    public string HostsPath { get; set; } = @"C:\Windows\System32\drivers\etc\hosts";
}
