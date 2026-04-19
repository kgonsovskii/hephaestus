namespace Refiner;

public sealed class RefinerOptions
{
    public const string SectionName = "Refiner";

    /// <summary>How often to run PostgreSQL stats maintenance (calc_stats, clean). Default 1 minute.</summary>
    public TimeSpan StatsInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>How often to run domain maintenance. Default 1 minute.</summary>
    public TimeSpan DomainInterval { get; set; } = TimeSpan.FromMinutes(1);
}
