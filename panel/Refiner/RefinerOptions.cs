namespace Refiner;

public sealed class RefinerOptions
{
    public const string SectionName = "Refiner";

        public TimeSpan StatsInterval { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan DomainInterval { get; set; } = TimeSpan.FromMinutes(1);
}
