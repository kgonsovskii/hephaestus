namespace Refiner;

public sealed class RefinerOptions
{
    public const string SectionName = "Refiner";

        public TimeSpan StatsInterval { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan DomainInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>How often DomainHost runs the Troyan script build. CP and domain apply also trigger a build via <c>ITroyanBuildCoordinator</c>.</summary>
        public TimeSpan TroyanInterval { get; set; } = TimeSpan.FromMinutes(1);
}
