using System.Security.Cryptography.X509Certificates;
using Commons;
using Cloner;
using cp;
using Db;
using Domain;
using DomainHost;
using Refiner;

var builder = WebApplication.CreateBuilder(args);




builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true,
    reloadOnChange: true);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"),
        optional: true, reloadOnChange: true);
}

builder.AddCp();
builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddDbServices(builder.Configuration);
builder.Services.AddRefiner(builder.Configuration);
builder.Services.AddHostedService<RefinerBackgroundService>();
builder.Services.AddClonerRemoteInstall(builder.Configuration);

var hostOpts = builder.Configuration.GetRequiredSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
    ?? throw new InvalidOperationException(
        $"Failed to bind '{DomainHostOptions.SectionName}'. Check appsettings for typos in property names.");
DomainHostOptionsValidator.ValidateOrThrow(hostOpts);

var paths = HephaestusPathResolver.FromSnapshot(hostOpts);
var dataRoot = paths.ResolveHephaestusDataRootFromAppBase();
var webFull = paths.WebDirectory(dataRoot);
if (!Directory.Exists(webFull))
    throw new InvalidOperationException(
        $"DomainHost: web directory not found at '{webFull}' (Hephaestus data root '{dataRoot}').");

var pfxPath = paths.FileUnderCert(dataRoot);
if (!File.Exists(pfxPath))
    throw new InvalidOperationException($"DomainHost: certificate PFX not found: {pfxPath}. Run CertTool once.");

var httpPort = Math.Clamp(hostOpts.HttpPort, 1, 65535);
var httpsPort = Math.Clamp(hostOpts.HttpsPort, 1, 65535);

var pfxPassword = hostOpts.CertPfxPassword ?? "";

const X509KeyStorageFlags PfxKeyStorage = X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable;
var serverCert = string.IsNullOrEmpty(pfxPassword)
    ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null, keyStorageFlags: PfxKeyStorage)
    : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPassword, PfxKeyStorage);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(httpPort);
    options.ListenAnyIP(httpsPort, listen => listen.UseHttps(serverCert));
});


builder.Services.AddSingleton<IWebFileResolver, WebFileResolver>();
builder.Services.AddSingleton<WebStaticRevision>();
builder.Services.AddSingleton<DomainHostRequestHandler>();
builder.Services.AddHostedService<WebRootFileWatcherHostedService>();


var app = builder.Build();


app.UseCpSite();
app.UseRouting();
app.UseMiddleware<DomainHostMiddleware>();
app.MapFallback(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    await ctx.Response.WriteAsync("Not found").ConfigureAwait(false);
});

await app.RunAsync();
