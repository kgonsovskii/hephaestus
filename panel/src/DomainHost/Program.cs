using System.Security.Cryptography.X509Certificates;
using Commons;
using cp;
using Db;
using Domain;
using DomainHost;
using Refiner;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json is linked from Commons and copied to output (BaseDirectory), but the project ContentRoot
// usually has no physical file, so Technitium/Refiner/ConnectionStrings never bind. RefinerTool avoids this by
// setting ContentRoot to BaseDirectory. Merge the built copy so DNS (Technitium) and DB stats see real config.
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

var hostOpts = builder.Configuration.GetSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
    ?? new DomainHostOptions();

var maxSteps = Math.Clamp(hostOpts.WebRootSearchMaxAscents, 1, 200);
var dataDirName = hostOpts.HephaestusDataDirectoryName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (dataDirName.Length == 0)
    dataDirName = HephaestusRepoPaths.DefaultDataDirectoryName;
var dataRoot = HephaestusRepoPaths.ResolveHephaestusDataRootFromAppBase(dataDirName, maxSteps);

var webFolder = hostOpts.WebRoot.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (webFolder.Length == 0)
    webFolder = "web";
var webFull = HephaestusRepoPaths.WebDirectory(dataRoot, webFolder);
if (!Directory.Exists(webFull))
    throw new InvalidOperationException(
        $"DomainHost: web directory not found at '{webFull}' (Hephaestus data root '{dataRoot}').");

var certDir = hostOpts.CertDirectoryName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (certDir.Length == 0)
    certDir = "cert";
var pfxName = hostOpts.CertPfxFileName.Trim();
if (pfxName.Length == 0)
    pfxName = "hephaestus.pfx";

var pfxPath = HephaestusRepoPaths.FileUnderCert(dataRoot, certDir, pfxName);
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


builder.Services.AddSingleton<IWebFileResolver, WebFileResolver>();
builder.Services.AddSingleton<WebStaticRevision>();
builder.Services.AddSingleton<DomainHostRequestHandler>();
builder.Services.AddHostedService<WebRootFileWatcherHostedService>();


var app = builder.Build();

// /cp must work for any Host (localhost, 127.0.0.1, raw IP, or vhost); branch runs before routing + domain static host.
app.UseCpSite();
app.UseRouting();
app.UseMiddleware<DomainHostMiddleware>();
app.MapFallback(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    await ctx.Response.WriteAsync("Not found").ConfigureAwait(false);
});

await app.RunAsync();
