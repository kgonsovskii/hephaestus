namespace Domain;

public sealed class TechnitiumOptions
{
    public const string SectionName = "Technitium";

    /// <summary>When false, <see cref="DomainMaintenance"/> does not call the DNS API.</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL of the Technitium web UI / API, e.g. http://127.0.0.1:5380</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:5380";

    public string User { get; set; } = "admin";

    public string Password { get; set; } = "admin";

    /// <summary>TTL in seconds for created/updated A/AAAA records when not overridden.</summary>
    public int RecordTtlSeconds { get; set; } = 300;

    /// <summary>Sign primary zones with DNSSEC (<c>api/zones/dnssec/sign</c>) when created or still unsigned.</summary>
    public bool DnssecEnabled { get; set; } = true;

    /// <summary>Add/update forward A/AAAA with PTR and create reverse zone when needed (<c>ptr</c>, <c>createPtrZone</c>).</summary>
    public bool PtrEnabled { get; set; } = true;

    public string DnssecSignAlgorithm { get; set; } = "ECDSA";

    public string DnssecCurve { get; set; } = "P256";

    /// <summary>NSEC or NSEC3</summary>
    public string DnssecNxProof { get; set; } = "NSEC3";

    public int DnssecDnsKeyTtl { get; set; } = 86400;

    public int DnssecZskRolloverDays { get; set; } = 30;

    public int DnssecNsec3Iterations { get; set; }

    public int DnssecNsec3SaltLength { get; set; }
}
