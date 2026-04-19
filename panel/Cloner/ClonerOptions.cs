namespace Cloner;

public sealed class ClonerOptions
{
    public const string SectionName = "Cloner";

    /// <summary>Used on DomainHost only: repo root for <c>install/install-remote.txt</c> when serving <c>/internal/install-remote</c>. Empty = walk up from app base.</summary>
    public string RepoRoot { get; set; } = "";

    /// <summary>Base URL of DomainHost (no trailing path). Clone always POSTs to <c>/internal/install-remote</c> here — no local sshpass/ssh on the machine running the control panel.</summary>
    public string DomainHostExecutorBaseUrl { get; set; } = "";

    /// <summary>Must match <c>DomainHost:ClonerInternalApiKey</c> on DomainHost.</summary>
    public string DomainHostExecutorApiKey { get; set; } = "";

    /// <summary>Development only: skip TLS server certificate validation for <see cref="DomainHostExecutorBaseUrl"/>.</summary>
    public bool DomainHostExecutorSkipTlsValidation { get; set; }
}
