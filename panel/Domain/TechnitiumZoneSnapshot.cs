namespace Domain;

public sealed class TechnitiumZoneSnapshot
{
    public required string Name { get; init; }

    public string Type { get; init; } = "";

    public bool Internal { get; init; }

        public string DnssecStatus { get; init; } = "";
}
