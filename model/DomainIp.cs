using System.Text.Json.Serialization;

namespace model;

public class DomainIp: BaseModel
{
    [JsonPropertyName("index")]
    public string Index { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("ip")]
    public string IP { get; set; }

    [JsonPropertyName("domains")]
    public List<string> Domains { get; set; } = new List<string>();
    
    //FTP
    [JsonPropertyName("ftp")] public string Ftp => $@"ftp://ftp_{Name.Replace(' ','_')}:Abc12345!@{IP}";
    [JsonPropertyName("ftpAsHttp")] public string FtpAsHttp => $@"http://{IP}/ftp";
    
    [JsonIgnore] public string? Result { get; set; }
    
    [JsonPropertyName("enabled")]
    public bool Enabled {get; set;}
    
    public static string _WWW => @"C:\inetpub\wwwroot\";
    public static string _ADS => Path.Combine(_WWW, "ads");

    [JsonPropertyName("ads")] public string Ads => Path.Combine(_ADS, Name.Replace(' ','_'));

    public void Assign(DomainIp domainIp, bool withDomains)
    {
        Name = domainIp.Name;
        IP = domainIp.IP;
        Enabled = domainIp.Enabled;
        if (withDomains)
            Domains = domainIp.Domains;
    }
    
    protected override void InternalRefresh()
    {
    }
}