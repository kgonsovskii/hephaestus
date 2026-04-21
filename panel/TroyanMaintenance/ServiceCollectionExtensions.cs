using Microsoft.Extensions.DependencyInjection;

namespace TroyanMaintenance;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTroyanBuildMaintenance(this IServiceCollection services)
    {
        services.AddSingleton<ITroyanBuildMaintenance, TroyanBuildMaintenance>();
        return services;
    }
}
