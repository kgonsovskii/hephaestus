using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace model;

    public class TabModel: BaseModel
    {
        public string Random()
        {
            return VbsRandomer.GenerateRandomVariableName(10);
        }

        public TabModel(ServerModel serverModel) : base(serverModel)
        {
        }
        
        private string _id;
       
        [JsonIgnore] public string Server => ServerModel.Server;
        
        [JsonPropertyName("disableVirus")] public bool DisableVirus
        {
            get
            {
                return ServerModel.DisableVirus;
            }
            set
            {
                ServerModel.DisableVirus = value;
            }
        }
        
        [JsonPropertyName("trackSerie")] public string TrackSerie
        {
            get
            {
                return ServerModel.TrackSerie;
            }
            set
            {
                ServerModel.TrackSerie = value;
            }
        }
        
        [JsonPropertyName("trackDesktop")] public bool TrackDesktop
        {
            get
            {
                return ServerModel.TrackDesktop;
            }
            set
            {
                ServerModel.TrackDesktop = value;
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
                return ServerModel.LandingAuto;
            }
            set
            {
                ServerModel.LandingAuto = value;
            }
        }
        
        [JsonPropertyName("landingName")]
        public string LandingName
        {
            get
            {
                return ServerModel.LandingName;
            }
            set
            {
                ServerModel.LandingName = value;
            }
        }
        
        [JsonPropertyName("landingFtp")]
        public string LandingFtp
        {
            get
            {
                return ServerModel.LandingFtp;
            }
            set
            {
                ServerModel.LandingFtp = value;
            }
        }

        [JsonPropertyName("pushesForce")] public bool PushesForce
        {
            get => ServerModel.PushesForce;
            set => ServerModel.PushesForce = value;
        }
        [JsonPropertyName("pushes")] public List<string> Pushes => ServerModel.Pushes;

        [JsonPropertyName("startDownloadsForce")] public bool StartDownloadsForce
        {
            get => ServerModel.StartDownloadsForce;
            set => ServerModel.StartDownloadsForce = value;
        }
        [JsonPropertyName("startDownloads")] public List<string> StartDownloads => ServerModel.StartDownloads;

        [JsonPropertyName("startUrlsForce")] public bool StartUrlsForce
        {
            get => ServerModel.StartUrlsForce;
            set =>ServerModel.StartUrlsForce = value;
        }
        [JsonPropertyName("startUrls")] public List<string> StartUrls => ServerModel.StartUrls;

        [JsonPropertyName("frontForce")] public bool FrontForce
        {
            get => ServerModel.FrontForce;
            set => ServerModel.FrontForce = value;
        }
        [JsonPropertyName("front")] public List<string> Front => ServerModel.Front;

        [JsonPropertyName("extractIconFromFront")]
        public bool ExtractIconFromFront => ServerModel.ExtractIconFromFront;

        [JsonPropertyName("embeddingsForce")] public bool EmbeddingsForce
        {
            get => ServerModel.EmbeddingsForce;
            set => ServerModel.EmbeddingsForce = value;
        }
        [JsonPropertyName("embeddings")] public List<string> Embeddings => ServerModel.Embeddings;
        
        protected override void InternalRefresh()
        {
        }
    }
