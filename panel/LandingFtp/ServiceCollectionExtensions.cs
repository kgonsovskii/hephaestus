using Microsoft.Extensions.DependencyInjection;

namespace LandingFtp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLandingFtpMaintenance(this IServiceCollection services)
    {
        services.AddSingleton<ILandingFtpMaintenance, LandingFtpMaintenance>();
        return services;
    }
}
