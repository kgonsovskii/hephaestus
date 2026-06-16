namespace Refiner;

public sealed class RefinerOptions
{
    public const string SectionName = "Refiner";

        public TimeSpan StatsInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>How often domain catalog reload and Technitium DNS sync run; CP apply also wakes via <c>NotifyHostsChanged</c>.</summary>
        public TimeSpan DomainInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>How often Troyan build runs; <c>NotifyHostsChanged</c> on the shared signal also wakes it (CP apply / domain save).</summary>
        public TimeSpan TroyanInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>Periodic landing FTP upload; CP apply runs upload in the Troyan loop immediately after rebuild.</summary>
        public TimeSpan LandingFtpInterval { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>How often sibling <c>hephaestus_data</c> is synced from GitHub; CP apply also wakes via <c>NotifyHostsChanged</c>.</summary>
        public TimeSpan HephaestusDataInterval { get; set; } = TimeSpan.FromHours(24);

        /// <summary>How often <c>server.json</c> network fields are refreshed from live interfaces (also runs once when Refiner starts).</summary>
        public TimeSpan ServerNetworkInterval { get; set; } = TimeSpan.FromMinutes(5);
}
