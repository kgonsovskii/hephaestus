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

        var maxSteps = Math.Clamp(opts.WebRootSearchMaxAscents, 1, 50);
        var start = Path.GetFullPath(env.ContentRootPath);

        WebRootFullPath = FindWebRoot(start, folderName, maxSteps)
            ?? throw new InvalidOperationException(
                $"DomainHost: could not find a '{folderName}' directory within {maxSteps} level(s) above content root '{start}'.");

        logger.LogInformation("Web content root: {WebRoot}", WebRootFullPath);
    }

    public string WebRootFullPath { get; }

    private static string? FindWebRoot(string startDirectory, string folderName, int maxAscents)
    {
        var current = startDirectory;
        for (var step = 0; step < maxAscents; step++)
        {
            var candidate = Path.GetFullPath(Path.Combine(current, folderName));
            if (Directory.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        return null;
    }
}
