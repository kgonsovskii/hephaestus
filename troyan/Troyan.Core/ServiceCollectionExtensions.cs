using Microsoft.Extensions.DependencyInjection;

namespace Troyan.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTroyanCore(this IServiceCollection services)
    {
        services.AddSingleton<IPowerShellObfuscator, PowerShellObfuscator>();
        services.AddSingleton<ITroyanPlainVbsEmitter, TroyanPlainVbsEmitter>();
        services.AddSingleton<ITroyanBuildRunner, TroyanBuildRunner>();
        services.AddSingleton<ITroyanBuildCoordinator, TroyanBuildCoordinator>();
        return services;
    }
}
