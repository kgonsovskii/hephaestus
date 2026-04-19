using Microsoft.Extensions.Logging;

namespace Cloner;

internal static class RepoRootResolver
{
    internal static string Resolve(string? configuredRoot, ILogger logger)
    {
        var t = configuredRoot?.Trim() ?? "";
        if (t.Length > 0)
        {
            var install = Path.Combine(t, "install");
            if (Directory.Exists(install) && File.Exists(Path.Combine(install, "install-remote.sh")))
                return Path.GetFullPath(t);
        }

        var start = Path.GetFullPath(AppContext.BaseDirectory);
        var current = start;
        for (var i = 0; i < 12; i++)
        {
            var install = Path.Combine(current, "install");
            if (Directory.Exists(install) && File.Exists(Path.Combine(install, "install-remote.sh")))
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        logger.LogWarning(
            "Cloner: could not find repo root (folder with install/install-remote.sh). Set {Section}:{Prop} in appsettings.",
            ClonerOptions.SectionName,
            nameof(ClonerOptions.RepoRoot));
        return start;
    }
}
