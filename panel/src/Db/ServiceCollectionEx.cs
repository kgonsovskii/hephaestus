using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Db;

public static class ServiceCollectionEx
{
    public static void AddDbServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IStatsMaintenance, StatsMaintenance>();
    }
}
