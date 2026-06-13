using System.Security.Cryptography.X509Certificates;
using Cloner;
using Commons;
using cp;
using DataFtp;
using Db;
using Domain;
using Git;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using model;
using Refiner;

namespace DomainHost;

internal static class DomainHostRunner
{
    private static readonly ILogger BootLogger = LoggerFactory.Create(b =>
    {
        b.AddSimpleConsole(o => o.SingleLine = true);
        b.SetMinimumLevel(LogLevel.Information);
    }).CreateLogger("DomainHost");

    public static async Task RunForeverAsync(string[] args, CancellationToken cancellationToken = default)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };

        while (!stop.Token.IsCancellationRequested)
        {
            var retryDelaySeconds = ReadRetryDelaySeconds(args);
            WebApplication? app = null;
            try
            {
                app = Build(args);
                LogPanelPaths(app);
                await app.RunAsync(stop.Token).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (stop.Token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                await SafeDisposeAsync(app).ConfigureAwait(false);
                app = null;
                BootLogger.LogErrorMessage(ex, "DomainHost stopped; retrying in {RetrySeconds}s", retryDelaySeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stop.Token.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private static int ReadRetryDelaySeconds(string[] args)
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .AddCommandLine(args)
                .Build();

            var seconds = config.GetSection(DomainHostOptions.SectionName).GetValue<int?>(nameof(DomainHostOptions.RetryDelaySeconds));
            if (seconds is >= 1 and <= 3600)
                return seconds.Value;
        }
        catch (Exception ex)
        {
            BootLogger.LogWarningMessage(ex, "Could not read DomainHost:RetryDelaySeconds from appsettings");
        }

        return 5;
    }

    private static async Task SafeDisposeAsync(WebApplication? app)
    {
        if (app is null)
            return;
        try
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception disposeEx)
        {
            BootLogger.LogWarningMessage(disposeEx, "DomainHost dispose failed");
        }
    }

    private static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (OperatingSystem.IsWindows())
            builder.Host.UseWindowsService();

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        builder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.None);

        builder.Services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true,
            reloadOnChange: true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"),
                optional: true, reloadOnChange: true);
        }

        builder.Services.AddDomainServices(builder.Configuration);
        builder.Services.AddDataFtp(builder.Configuration);
        builder.AddCp();
        builder.Services.AddDbServices(builder.Configuration);
        builder.Services.AddRefiner(builder.Configuration);
        builder.Services.AddHostedService<RefinerBackgroundService>();
        builder.Services.AddClonerRemoteInstall(builder.Configuration);

        var hostOpts = builder.Configuration.GetRequiredSection(DomainHostOptions.SectionName).Get<DomainHostOptions>()
            ?? throw new InvalidOperationException(
                $"Failed to bind '{DomainHostOptions.SectionName}'. Check appsettings for typos in property names.");
        DomainHostOptionsValidator.ValidateOrThrow(hostOpts);

        var paths = HephaestusPathResolver.FromSnapshot(hostOpts);
        paths.EnsureDirectoriesFromAppBase();
        try
        {
            HephaestusDataGitRunner.Run(paths, BootLogger);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("DomainHost: hephaestus_data git bootstrap failed.", ex);
        }

        var repoRoot = paths.ResolveRepositoryRootFromAppBase();
        var dataRoot = paths.ResolveHephaestusDataRootFromAppBase();
        var webFull = paths.WebDirectory(dataRoot);

        var pfxPath = paths.FileUnderCert(repoRoot);
        if (!File.Exists(pfxPath))
            throw new InvalidOperationException($"DomainHost: certificate PFX not found: {pfxPath}. Run CertTool once.");

        var domainsIgnorePath = paths.DomainsIgnorePathFromAppBase();
        if (!File.Exists(domainsIgnorePath))
            throw new InvalidOperationException($"DomainHost: domains-ignore file not found: {domainsIgnorePath}");

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

        app.UseMiddleware<InstallRemoteInternalMiddleware>();
        app.UseCpSite();
        app.UseRouting();
        app.UseMiddleware<DomainHostMiddleware>();
        app.MapFallback(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsync("Not found").ConfigureAwait(false);
        });

        return app;
    }

    private static void LogPanelPaths(WebApplication app)
    {
        var panelPaths = app.Services.GetRequiredService<IPanelServerPaths>();
        var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DomainHost");
        log.LogInformation(
            "Panel paths (server {ServerId}): HephaestusDataRoot={HephaestusDataRoot}; RootData={RootData}; UserDataDir={UserDataDir}; DataFile={DataFile}; RepositoryRoot={RepositoryRoot}",
            PanelServerIdentity.DefaultKey,
            panelPaths.HephaestusDataRoot,
            panelPaths.RootData,
            panelPaths.UserDataDir,
            panelPaths.DataFile,
            panelPaths.RootDir);
    }
}
