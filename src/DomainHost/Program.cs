using System.Security.Cryptography.X509Certificates;
using DomainHost.Configuration;
using DomainHost.Data;
using DomainHost.Middleware;
using DomainHost.Services;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DomainHostOptions>(
    builder.Configuration.GetSection(DomainHostOptions.SectionName));

var hostOpts = builder.Configuration.GetSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
    ?? new DomainHostOptions();

var webRootFullPath = FindWebRoot(
    Path.GetFullPath(builder.Environment.ContentRootPath),
    hostOpts.WebRoot,
    hostOpts.WebRootSearchMaxAscents)
    ?? throw new InvalidOperationException(
        $"DomainHost: could not find folder '{hostOpts.WebRoot}' with '{hostOpts.DomainsFileName}' within {hostOpts.WebRootSearchMaxAscents} ascents of '{builder.Environment.ContentRootPath}'.");

var certDir = hostOpts.CertDirectoryName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (certDir.Length == 0)
    certDir = "cert";
var pfxName = hostOpts.CertPfxFileName.Trim();
if (pfxName.Length == 0)
    pfxName = "hephaestus.pfx";

var pfxPath = Path.GetFullPath(Path.Combine(webRootFullPath, "..", certDir, pfxName));
if (!File.Exists(pfxPath))
    throw new InvalidOperationException($"DomainHost: certificate PFX not found: {pfxPath}. Run CertTool once.");

var httpsPort = Math.Clamp(hostOpts.HttpsPort, 1, 65535);

var serverCert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null, keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.ServerCertificate = serverCert;
        https.ClientCertificateMode = ClientCertificateMode.NoCertificate;
    });
});
builder.WebHost.UseUrls($"https://0.0.0.0:{httpsPort}");

builder.Services.AddSingleton<DomainCatalog>();
builder.Services.AddSingleton<IDomainCatalog>(sp => sp.GetRequiredService<DomainCatalog>());
builder.Services.AddSingleton<IWebContentPathProvider, WebContentPathProvider>();
builder.Services.AddSingleton<IDomainRepository, JsonFileDomainRepository>();
builder.Services.AddSingleton<IWebFileResolver, WebFileResolver>();
builder.Services.AddSingleton<DomainHostRequestHandler>();
builder.Services.AddHostedService<DomainCatalogRefreshService>();

var app = builder.Build();

app.UseMiddleware<DomainHostMiddleware>();
app.MapFallback(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    await ctx.Response.WriteAsync("Not found").ConfigureAwait(false);
});

await app.RunAsync();

static string? FindWebRoot(string startDirectory, string folderName, int maxAscents)
{
    var name = folderName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    if (name.Length == 0)
        name = "web";

    var current = startDirectory;
    for (var step = 0; step < maxAscents; step++)
    {
        var candidate = Path.GetFullPath(Path.Combine(current, name));
        if (Directory.Exists(candidate))
            return candidate;

        var parent = Directory.GetParent(current);
        if (parent == null)
            break;
        current = parent.FullName;
    }

    return null;
}
