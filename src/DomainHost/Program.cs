using System.Security.Cryptography.X509Certificates;
using Commons;
using DomainHost.Configuration;
using DomainHost.Data;
using DomainHost.Middleware;
using DomainHost.Services;
using Refiner;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DomainHostOptions>(
    builder.Configuration.GetSection(DomainHostOptions.SectionName));
builder.Services.Configure<RefinerOptions>(
    builder.Configuration.GetSection(RefinerOptions.SectionName));
builder.Services.AddSingleton<IStatsMaintenance, StatsMaintenance>();
builder.Services.AddSingleton<IDomainMaintenance, DomainMaintenance>();
builder.Services.AddHostedService<RefinerBackgroundService>();

var hostOpts = builder.Configuration.GetSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
    ?? new DomainHostOptions();

var maxSteps = Math.Clamp(hostOpts.WebRootSearchMaxAscents, 1, 200);
var start = Path.GetFullPath(builder.Environment.ContentRootPath);
var repoRoot = HephaestusRepoPaths.ResolveRepositoryRoot(start, HephaestusRepoPaths.DefaultMarkerFileName, maxSteps);

var webFolder = hostOpts.WebRoot.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (webFolder.Length == 0)
    webFolder = "web";
var webFull = HephaestusRepoPaths.WebDirectory(repoRoot, webFolder);
if (!Directory.Exists(webFull))
    throw new InvalidOperationException(
        $"DomainHost: web directory not found at '{webFull}' (repository root '{repoRoot}').");

var certDir = hostOpts.CertDirectoryName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (certDir.Length == 0)
    certDir = "cert";
var pfxName = hostOpts.CertPfxFileName.Trim();
if (pfxName.Length == 0)
    pfxName = "hephaestus.pfx";

var pfxPath = HephaestusRepoPaths.FileUnderCert(repoRoot, certDir, pfxName);
if (!File.Exists(pfxPath))
    throw new InvalidOperationException($"DomainHost: certificate PFX not found: {pfxPath}. Run CertTool once.");

var httpPort = Math.Clamp(hostOpts.HttpPort, 1, 65535);
var httpsPort = Math.Clamp(hostOpts.HttpsPort, 1, 65535);

var pfxPassword = hostOpts.CertPfxPassword ?? "";
// EphemeralKeySet breaks TLS handshakes for Kestrel HTTPS on Windows (Schannel); use a persisted key handle.
const X509KeyStorageFlags PfxKeyStorage = X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable;
var serverCert = string.IsNullOrEmpty(pfxPassword)
    ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null, keyStorageFlags: PfxKeyStorage)
    : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPassword, PfxKeyStorage);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(httpPort);
    options.ListenAnyIP(httpsPort, listen => listen.UseHttps(serverCert));
});

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
