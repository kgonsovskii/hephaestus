namespace model;

/// <summary>Resolved panel paths (Hephaestus data root, repository clone, per-server dirs, tool exes).</summary>
public interface IPanelServerPaths
{
    string HephaestusDataRoot { get; }

    /// <summary>Root directory containing one folder per server name (server.json, etc.).</summary>
    string RootData { get; }

    string RootDir { get; }
    string SourceCertDir { get; }
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

    string ServerDir(string serverName);
    string DataFile(string serverName);
    string UserDataDir(string server);
    string UserDataFile(string server, string file);
    string UserDataBody(string server);
}
