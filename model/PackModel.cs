using System.Text.Json.Serialization;

namespace model;

public class PackModel: BaseModel
{
    public PackModel(ServerModel serverModel) : base(serverModel)
    {
    }

    public PackModel()
    {
    }

    [JsonPropertyName("packFolder")]
    public string PackFolder => Path.Combine(ServerModel?.UserDataDir, "packs");

    [JsonPropertyName("packTemplateUrl")]
    public string PackTemplateUrl
    {
        get
        {
            var url = ServerModel.Alias;
            if (string.IsNullOrEmpty(url))
                url = ServerModel.ServerIp;
            url = "http://" + url + "/pack/envelope";
            return url;
        }
    }

    [JsonIgnore]
    public string PackLog { get; set; } = string.Empty;

    [JsonPropertyName("items")] public List<PackItem> Items { get; set; } = new List<PackItem>() { };

    protected override void InternalRefresh()
    {
    }
}

public class PackItem: BaseModel
{
    protected PackModel PackModel => (PackModel) Parent ?? new PackModel();
    public PackItem(PackModel packModel) : base(packModel)
    {
    }
    public PackItem() : base()
    {
    }
    [JsonPropertyName("packFolder")]
    public string PackFolder => Path.Combine(PackModel.PackFolder, Id);
    
    [JsonPropertyName("packFileVbs")]
    public string PackFileVbs => Path.Combine(PackFolder, Name + ".vbs");
    
    [JsonPropertyName("packFileExe")]
    public string PackFileExe => Path.Combine(PackFolder, Name + ".exe");


    [JsonPropertyName("id")]
    public string Id => UrlHelper.HashUrlTo5Chars(OriginalUrl);


    [JsonPropertyName("name")] public string Name => UrlHelper.GetFileNameFromUrl(OriginalUrl, PackModel.PackTemplateUrl);
    
    [JsonPropertyName("originalUrl")]
    public string OriginalUrl { get; set; }
    
    [JsonPropertyName("icon")]
    public string Icon { get; set; }
    
    [JsonPropertyName("iconFile")]
    public string IconFile => Path.Combine(PackFolder, Name + ".ico");

    [JsonPropertyName("urlExe")] public string UrlExe => PackModel.PackTemplateUrl + "?type=exe&url=" + OriginalUrl;
    [JsonPropertyName("urlVbs")] public string UrlVbs => PackModel.PackTemplateUrl + "?type=vbs&url=" + OriginalUrl;

    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("date")] public string Date { get; set; } = "";

    protected override void InternalRefresh()
    {
    }


    public void Validate()
    {
        Refresh();
        if (!File.Exists(PackFileExe) || !File.Exists(PackFileVbs))
        {
            if (!Directory.Exists(PackFolder))
                Directory.CreateDirectory(PackFolder);
        }
        if (!string.IsNullOrEmpty(Icon) && !File.Exists(IconFile))
        {
            UrlHelper.DownloadFile(Icon, IconFile);
        }
    }
}
