using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refiner;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.Configure<RefinerOptions>(
        builder.Configuration.GetSection(RefinerOptions.SectionName));
    builder.Services.AddSingleton<IStatsMaintenance, StatsMaintenance>();
    builder.Services.AddSingleton<IDomainMaintenance, DomainMaintenance>();

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
