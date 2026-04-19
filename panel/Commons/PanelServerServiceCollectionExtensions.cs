using Microsoft.Extensions.DependencyInjection;
using model;

namespace Commons;

public static class PanelServerServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IPanelServerPaths"/>, <see cref="ServerModelLoader"/>, and <see cref="ServerService"/>. Requires <see cref="IHephaestusPathResolver"/> and bound <see cref="DomainHostOptions"/> (same pattern as the panel host).</summary>
    public static IServiceCollection AddPanelServerStack(this IServiceCollection services)
    {
        services.AddSingleton<IPanelServerPaths, PanelServerPaths>();
        services.AddSingleton<ServerModelLoader>();
        services.AddSingleton<ServerService>();
        return services;
    }
}
