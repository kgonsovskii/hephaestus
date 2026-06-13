using Commons;
using Microsoft.Extensions.Logging;

namespace Domain;

public interface IWebContentPathProvider
{
    string DataRootFullPath { get; }

    string WebRootFullPath { get; }
}

public sealed class WebContentPathProvider : IWebContentPathProvider
{
    public WebContentPathProvider(
        IHephaestusPathResolver paths,
        ILogger<WebContentPathProvider> logger)
    {
        paths.EnsureDirectoriesFromAppBase();
        var dataRoot = paths.ResolveHephaestusDataRootFromAppBase();
        var webFull = paths.WebDirectory(dataRoot);

        DataRootFullPath = dataRoot;
        WebRootFullPath = webFull;
        logger.LogInformation("Hephaestus data root: {DataRoot}", DataRootFullPath);
        logger.LogInformation("Web content root: {WebRoot}", WebRootFullPath);
    }

    public string DataRootFullPath { get; }

    public string WebRootFullPath { get; }
}
