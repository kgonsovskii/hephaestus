using Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Domain;

public interface IWebContentPathProvider
{
    /// <summary>Resolved <c>hephaestus_data</c> directory (<c>domains.json</c> lives here).</summary>
    string DataRootFullPath { get; }

    /// <summary>Static files under <c>hephaestus_data/web</c> (or configured <see cref="DomainHostOptions.WebRoot"/>).</summary>
    string WebRootFullPath { get; }
}

public sealed class WebContentPathProvider : IWebContentPathProvider
{
    public WebContentPathProvider(
        IOptions<DomainHostOptions> options,
        ILogger<WebContentPathProvider> logger)
    {
        var opts = options.Value;
        var folderName = opts.WebRoot.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (folderName.Length == 0)
            folderName = "web";

        var dataDirName = opts.HephaestusDataDirectoryName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (dataDirName.Length == 0)
            dataDirName = HephaestusRepoPaths.DefaultDataDirectoryName;

        var maxSteps = Math.Clamp(opts.WebRootSearchMaxAscents, 1, 200);
        var dataRoot = HephaestusRepoPaths.ResolveHephaestusDataRootFromAppBase(dataDirName, maxSteps);
        var webFull = HephaestusRepoPaths.WebDirectory(dataRoot, folderName);
        if (!Directory.Exists(webFull))
            throw new InvalidOperationException(
                $"DomainHost: web directory not found at '{webFull}' (Hephaestus data root '{dataRoot}').");

        DataRootFullPath = dataRoot;
        WebRootFullPath = webFull;
        logger.LogInformation("Hephaestus data root: {DataRoot}", DataRootFullPath);
        logger.LogInformation("Web content root: {WebRoot}", WebRootFullPath);
    }

    public string DataRootFullPath { get; }

    public string WebRootFullPath { get; }
}
