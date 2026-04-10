using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public static class ServiceCollectionEx
{
    public static void AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDomainRepository, JsonFileDomainRepository>();
        services.Configure<DomainHostOptions>(
            configuration.GetSection(DomainHostOptions.SectionName));
        services.AddSingleton<IDomainMaintenance, DomainMaintenance>();
        services.AddSingleton<DomainCatalog>();
        services.AddSingleton<IDomainCatalog>(sp => sp.GetRequiredService<DomainCatalog>());
        services.AddSingleton<IWebContentPathProvider, WebContentPathProvider>();
        services.AddHostedService<DomainCatalogRefreshService>();
    }
}
