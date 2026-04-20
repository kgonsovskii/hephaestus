namespace model;

/// <summary>Resolved panel paths (Hephaestus data root, repository clone, single server home under <see cref="PanelServerIdentity.DefaultKey"/>, tool exes).</summary>
public interface IPanelServerPaths
{
    string HephaestusDataRoot { get; }

    /// <summary>Root directory containing server homes (each named <see cref="PanelServerIdentity.DefaultKey"/>).</summary>
    string RootData { get; }

    /// <summary>Shared nested payloads for Troyan body (<c>xembeddings</c>), e.g. <c>C:\data\defaults</c> on Windows.</summary>
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

    /// <summary>Home directory for the only server (<c>.../default</c>).</summary>
    string ServerDir { get; }

    string DataFile { get; }

    /// <summary>Same as <see cref="ServerDir"/> (user payload lives in the server home).</summary>
    string UserDataDir { get; }

    string UserDataFile(string file);

    string UserDataBody { get; }
}
