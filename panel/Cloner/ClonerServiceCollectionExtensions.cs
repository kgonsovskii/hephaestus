using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cloner;

public static class ClonerServiceCollectionExtensions
{
    public static IServiceCollection AddClonerRemoteInstall(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClonerOptions>(configuration.GetSection(ClonerOptions.SectionName));
        services.AddSingleton<ClonerRemoteInstallService>();
        services.AddSingleton<IClonerRemoteInstall>(sp => sp.GetRequiredService<ClonerRemoteInstallService>());
        services.AddSingleton<InProcessClonerInstallExecutor>();
        services.AddSingleton<HttpDomainHostInstallRemoteExecutor>();
        services.AddSingleton<IClonerInstallExecutor, ClonerInstallExecutorRouter>();
        services
            .AddHttpClient(HttpDomainHostInstallRemoteExecutor.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var h = new SocketsHttpHandler();
                if (sp.GetRequiredService<IOptionsMonitor<ClonerOptions>>().CurrentValue.DomainHostExecutorSkipTlsValidation)
                {
                    h.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
                }

                return h;
            });
        services.AddHostedService<ClonerRemoteInstallHostedService>();
        return services;
    }
}
