namespace Domain;

public sealed class TechnitiumOptions
{
    public const string SectionName = "Technitium";

        public bool Enabled { get; set; }

        public string BaseUrl { get; set; } = "http://127.0.0.1:5380";

    public string User { get; set; } = "admin";

    public string Password { get; set; } = "admin";

        public bool ForwarderEnabled { get; set; } = true;

        public string Forwarders { get; set; } = "8.8.8.8";

        public string Recursion { get; set; } = "Allow";

        public bool DnssecEnabled { get; set; } = true;

        public bool PtrEnabled { get; set; } = true;

    public string DnssecSignAlgorithm { get; set; } = "ECDSA";

    public string DnssecCurve { get; set; } = "P256";

        public string DnssecNxProof { get; set; } = "NSEC3";

    public int DnssecDnsKeyTtl { get; set; } = 86400;

    public int DnssecZskRolloverDays { get; set; } = 30;

    public int DnssecNsec3Iterations { get; set; }

    public int DnssecNsec3SaltLength { get; set; }
}
