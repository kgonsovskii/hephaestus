using DomainHost.Configuration;
using DomainHost.Data;
using DomainHost.Middleware;
using DomainHost.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DomainHostOptions>(
    builder.Configuration.GetSection(DomainHostOptions.SectionName));

var domainOpts = builder.Configuration.GetSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
                 ?? new DomainHostOptions();

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel((ctx, opts) =>
    {
        opts.ListenAnyIP(80);
        if (domainOpts.Https.Enabled && !string.IsNullOrWhiteSpace(domainOpts.Https.PfxPath))
        {
            var pfx = Path.IsPathRooted(domainOpts.Https.PfxPath)
                ? domainOpts.Https.PfxPath
                : Path.GetFullPath(Path.Combine(ctx.HostingEnvironment.ContentRootPath, domainOpts.Https.PfxPath));
            if (File.Exists(pfx))
            {
                opts.ListenAnyIP(443,
                    listen => listen.UseHttps(pfx, domainOpts.Https.PfxPassword ?? ""));
            }
        }
    });
}

builder.Services.AddSingleton<DomainCatalog>();
builder.Services.AddSingleton<IDomainCatalog>(sp => sp.GetRequiredService<DomainCatalog>());
builder.Services.AddSingleton<IWebContentPathProvider, WebContentPathProvider>();
builder.Services.AddScoped<IDomainRepository, NpgsqlDomainRepository>();
builder.Services.AddSingleton<IWebFileResolver, WebFileResolver>();
builder.Services.AddSingleton<DomainHostRequestHandler>();
builder.Services.AddHostedService<DomainCatalogRefreshService>();

var app = builder.Build();

app.UseMiddleware<DomainHostMiddleware>();
app.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    await ctx.Response.WriteAsync("Not found").ConfigureAwait(false);
});

await app.RunAsync();
