using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Domain;

public static class ServiceCollectionEx
{
    public static void AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDomainRepository, JsonFileDomainRepository>();
        services.Configure<DomainHostOptions>(
            configuration.GetSection(DomainHostOptions.SectionName));
        services.Configure<TechnitiumOptions>(
            configuration.GetSection(TechnitiumOptions.SectionName));
        services.AddHttpClient<TechnitiumDnsClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<TechnitiumOptions>>().Value;
            var b = o.BaseUrl.Trim().TrimEnd('/');
            client.BaseAddress = new Uri(b + "/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddSingleton<IDomainMaintenance, DomainMaintenance>();
        services.AddSingleton<DomainCatalog>();
        services.AddSingleton<IDomainCatalog>(sp => sp.GetRequiredService<DomainCatalog>());
        services.AddSingleton<IWebContentPathProvider, WebContentPathProvider>();
        services.AddSingleton<IWebContentClassCatalog, WebContentClassCatalog>();
        services.AddHostedService<DomainCatalogRefreshService>();
    }
}
