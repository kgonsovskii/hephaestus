using DomainTool.Configuration;
using DomainTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.Configure<DomainToolOptions>(
        builder.Configuration.GetSection(DomainToolOptions.SectionName));

    builder.Services.AddSingleton<IDomainNameSource, JsonDomainNameSource>();
    builder.Services.AddSingleton<IHostsFileComposer, HostsFileComposer>();
    builder.Services.AddSingleton<HostsFileSyncService>();

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    using var host = builder.Build();
    var exit = await host.Services.GetRequiredService<HostsFileSyncService>()
        .RunAsync(CancellationToken.None)
        .ConfigureAwait(false);
    return exit;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
