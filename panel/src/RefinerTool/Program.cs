using Db;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refiner;

try
{
    // Default content root is Environment.CurrentDirectory, so `dotnet run` from repo root skips
    // appsettings.json copied next to the assembly in output/. BaseDirectory always matches the exe folder.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = AppContext.BaseDirectory,
        Args = args
    });

    builder.Services.AddDomainServices(builder.Configuration);
    builder.Services.AddDbServices(builder.Configuration);
    builder.Services.AddRefiner(builder.Configuration);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    using var host = builder.Build();

    var stats = host.Services.GetRequiredService<IStatsMaintenance>();
    var domain = host.Services.GetRequiredService<IDomainMaintenance>();

    await stats.RunAsync(CancellationToken.None).ConfigureAwait(false);
    await domain.RunAsync(CancellationToken.None).ConfigureAwait(false);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
