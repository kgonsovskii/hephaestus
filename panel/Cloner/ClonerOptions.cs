namespace Cloner;

public sealed class ClonerOptions
{
    public const string SectionName = "Cloner";

    /// <summary>Used on DomainHost only: repo root for <c>install/install-remote.txt</c> when serving <c>/internal/install-remote</c>. Empty = walk up from app base.</summary>
    public string RepoRoot { get; set; } = "";

    /// <summary>Base URL of DomainHost (no trailing path). Empty = <c>http://127.0.0.1:80</c> (same host).</summary>
    public string DomainHostExecutorBaseUrl { get; set; } = "";

    /// <summary>Skip TLS certificate validation when calling DomainHost over HTTPS.</summary>
    public bool DomainHostExecutorSkipTlsValidation { get; set; }
}
