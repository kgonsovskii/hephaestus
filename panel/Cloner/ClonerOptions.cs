namespace Cloner;

public sealed class ClonerOptions
{
    public const string SectionName = "Cloner";

    /// <summary>Optional absolute path to Hephaestus repo root (folder that contains <c>install/</c>). Empty = walk up from app base.</summary>
    public string RepoRoot { get; set; } = "";

    /// <summary><c>InProcess</c> runs SSH from this process (DomainHost/Linux). <c>DomainHostHttp</c> POSTs to DomainHost <c>/internal/install-remote</c> (e.g. control panel on Windows).</summary>
    public string Executor { get; set; } = "InProcess";

    /// <summary>Base URL of DomainHost, e.g. <c>https://hephaestus.example.com:443</c>. Used when <see cref="Executor"/> is <c>DomainHostHttp</c>.</summary>
    public string DomainHostExecutorBaseUrl { get; set; } = "";

    /// <summary>Must match <c>DomainHost:ClonerInternalApiKey</c> on the server.</summary>
    public string DomainHostExecutorApiKey { get; set; } = "";

    /// <summary>Development only: skip TLS server certificate validation for <see cref="DomainHostExecutorBaseUrl"/>.</summary>
    public bool DomainHostExecutorSkipTlsValidation { get; set; }
}
