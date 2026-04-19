using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloner;

public static class ClonerServiceCollectionExtensions
{
    public static IServiceCollection AddClonerRemoteInstall(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClonerOptions>(configuration.GetSection(ClonerOptions.SectionName));
        services.AddSingleton<ClonerRemoteInstallService>();
        services.AddSingleton<IClonerRemoteInstall>(sp => sp.GetRequiredService<ClonerRemoteInstallService>());
        services.AddHostedService<ClonerRemoteInstallHostedService>();
        return services;
    }
}
