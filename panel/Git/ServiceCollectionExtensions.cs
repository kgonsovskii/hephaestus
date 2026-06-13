using Microsoft.Extensions.DependencyInjection;

namespace Git;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHephaestusDataGitMaintenance(this IServiceCollection services)
    {
        services.AddSingleton<IHephaestusDataGitMaintenance, HephaestusDataGitMaintenance>();
        return services;
    }
}
