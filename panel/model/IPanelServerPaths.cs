namespace model;

/// <summary>Resolved panel paths (Hephaestus data root, repository clone, single server home under <see cref="PanelServerIdentity.DefaultKey"/>, tool exes).</summary>
public interface IPanelServerPaths
{
    string HephaestusDataRoot { get; }

    /// <summary>Root directory containing server homes (each named <see cref="PanelServerIdentity.DefaultKey"/>).</summary>
    string RootData { get; }

    /// <summary>Shared nested payloads for Troyan body (<c>xembeddings</c>), under the active profile root.</summary>
    string DefaultsEmbedDir { get; }

    string RootDir { get; }
    string SourceCertDir { get; }

    /// <summary>Canonical Hephaestus LAN TLS PFX (CertTool / DomainHost); copied into <see cref="UserDataDir"/> before Troyan embed.</summary>
    string HephaestusTlsPfxPath { get; }

    string CpDir { get; }
    string AdsDir { get; }
    string PhpDir { get; }
    string CertDir { get; }
    string SysDir { get; }
    string TroyanDir { get; }
    string TroyanScriptDir { get; }
    string TroyanVbsDir { get; }

    string TroyanBuilder { get; }
    string Packer { get; }
    string CertTool { get; }

    /// <summary>Panel server home (<c>hephaestus_data/{profile}/server</c>); holds <c>server.json</c> and user payload.</summary>
    string ServerDir { get; }

    string DataFile { get; }

    /// <summary>Same as <see cref="ServerDir"/> (user payload lives in the server home).</summary>
    string UserDataDir { get; }

    string UserDataFile(string file);

    string UserDataBody { get; }

    /// <summary>Creates Hephaestus data, server, web, and cert directories when missing.</summary>
    void EnsureLayout();
}
