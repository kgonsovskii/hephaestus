using LandingFtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TroyanMaintenance;

namespace Refiner;

public static class ServiceCollectionEx
{
    public static void AddRefiner(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RefinerOptions>(
            configuration.GetSection(RefinerOptions.SectionName));
        services.AddTroyanBuildMaintenance();
        services.AddLandingFtpMaintenance();
    }
}
