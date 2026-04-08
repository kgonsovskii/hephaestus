using Commons;
using DomainHost.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DomainHost.Services;

public sealed class WebContentPathProvider : IWebContentPathProvider
{
    public WebContentPathProvider(
        IHostEnvironment env,
        IOptions<DomainHostOptions> options,
        ILogger<WebContentPathProvider> logger)
    {
        var opts = options.Value;
        var folderName = opts.WebRoot.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (folderName.Length == 0)
            folderName = "web";

        var maxSteps = Math.Clamp(opts.WebRootSearchMaxAscents, 1, 200);
        var start = Path.GetFullPath(env.ContentRootPath);
        var repoRoot = HephaestusRepoPaths.ResolveRepositoryRoot(start, HephaestusRepoPaths.DefaultMarkerFileName, maxSteps);
        var webFull = HephaestusRepoPaths.WebDirectory(repoRoot, folderName);
        if (!Directory.Exists(webFull))
            throw new InvalidOperationException(
                $"DomainHost: web directory not found at '{webFull}' (repository root '{repoRoot}').");

        WebRootFullPath = webFull;
        logger.LogInformation("Web content root: {WebRoot}", WebRootFullPath);
    }

    public string WebRootFullPath { get; }
}
