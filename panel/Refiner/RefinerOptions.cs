namespace Refiner;

public sealed class RefinerOptions
{
    public const string SectionName = "Refiner";

        public TimeSpan StatsInterval { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan DomainInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>How often Troyan build runs; <c>NotifyHostsChanged</c> on the shared signal also wakes it (CP apply / domain save).</summary>
        public TimeSpan TroyanInterval { get; set; } = TimeSpan.FromMinutes(1);
}
