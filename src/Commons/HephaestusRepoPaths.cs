namespace Commons;

/// <summary>
/// Resolves the Hephaestus runtime data directory (<c>hephaestus_data</c>) by walking parents from a start path.
/// Prefer a folder <b>next to the repository</b> (same parent directory as the clone), e.g. <c>C:\soft\hephaestus_data</c>
/// beside <c>C:\soft\hephaestus</c>. Falls back to <c>./hephaestus_data</c> under a search directory if present.
/// Then composes <c>web</c>, <c>cert</c>, and root-level <c>domains.json</c> paths under that directory.
/// Repository root (marker file) is still used for build outputs and legacy tooling via <see cref="ResolveRepositoryRoot"/>.
/// </summary>
public static class HephaestusRepoPaths
{
    public const string DefaultMarkerFileName = "defaulticon.ico";

    /// <summary>Default name of the data directory (typically sibling of the repo folder, e.g. <c>…\hephaestus_data</c> next to <c>…\hephaestus</c>).</summary>
    public const string DefaultDataDirectoryName = "hephaestus_data";

    #region Repository root (build tree; defaulticon.ico)

    public static string? TryResolveRepositoryRoot(
        string startDirectory,
        string markerFileName = DefaultMarkerFileName,
        int maxParentAscents = 50)
    {
        var marker = string.IsNullOrWhiteSpace(markerFileName)
            ? DefaultMarkerFileName
            : markerFileName.Trim();

        var max = Math.Clamp(maxParentAscents, 1, 200);
        var current = Path.GetFullPath(startDirectory);

        for (var step = 0; step < max; step++)
        {
            if (File.Exists(Path.Combine(current, marker)))
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        return null;
    }

    public static string ResolveRepositoryRoot(
        string startDirectory,
        string markerFileName = DefaultMarkerFileName,
        int maxParentAscents = 50)
    {
        var marker = string.IsNullOrWhiteSpace(markerFileName)
            ? DefaultMarkerFileName
            : markerFileName.Trim();

        var max = Math.Clamp(maxParentAscents, 1, 200);
        return TryResolveRepositoryRoot(startDirectory, marker, max)
            ?? throw new InvalidOperationException(
                $"Could not find repository root: no directory within {max} ascents of '{Path.GetFullPath(startDirectory)}' contains marker file '{marker}'.");
    }

    #endregion

    #region Hephaestus data root (hephaestus_data: web, cert, domains.json at root)

    /// <summary>
    /// Walks upward from <paramref name="startDirectory"/> (at most <paramref name="maxParentAscents"/> steps).
    /// At each level, in order:
    /// <list type="number">
    /// <item>If the current directory is named <paramref name="dataDirectoryName"/>, returns it (paths inside the data tree).</item>
    /// <item>If <c>../dataDirectoryName</c> relative to the current directory’s parent exists (sibling of <c>current</c>), returns it — this is the usual layout: <c>…\hephaestus_data</c> beside <c>…\hephaestus</c>.</item>
    /// <item>If <c>./dataDirectoryName</c> under <c>current</c> exists, returns it (legacy in-repo data folder).</item>
    /// </list>
    /// </summary>
    public static string? TryResolveHephaestusDataRoot(
        string startDirectory,
        string dataDirectoryName = DefaultDataDirectoryName,
        int maxParentAscents = 50)
    {
        var name = NormalizeDataDirectorySegment(dataDirectoryName);
        var max = Math.Clamp(maxParentAscents, 1, 200);
        var current = Path.GetFullPath(startDirectory);

        for (var step = 0; step < max; step++)
        {
            if (Directory.Exists(current))
            {
                var leaf = Path.GetFileName(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.Equals(leaf, name, StringComparison.OrdinalIgnoreCase))
                    return current;

                var parent = Directory.GetParent(current);
                if (parent != null)
                {
                    var sibling = Path.Combine(parent.FullName, name);
                    if (Directory.Exists(sibling))
                        return Path.GetFullPath(sibling);
                }

                var child = Path.Combine(current, name);
                if (Directory.Exists(child))
                    return Path.GetFullPath(child);
            }

            var nextParent = Directory.GetParent(current);
            if (nextParent == null)
                break;
            current = nextParent.FullName;
        }

        return null;
    }

    public static string ResolveHephaestusDataRoot(
        string startDirectory,
        string dataDirectoryName = DefaultDataDirectoryName,
        int maxParentAscents = 50)
    {
        var name = NormalizeDataDirectorySegment(dataDirectoryName);
        var max = Math.Clamp(maxParentAscents, 1, 200);
        return TryResolveHephaestusDataRoot(startDirectory, name, max)
            ?? throw new InvalidOperationException(
                $"Could not find Hephaestus data directory '{name}' within {max} parent ascents of '{Path.GetFullPath(startDirectory)}'. " +
                $"Create it next to the repository folder (same parent as the clone), e.g. ..\\{name} beside ..\\hephaestus, with ..\\{name}\\web, ..\\{name}\\cert, and domains.json at ..\\{name}\\.");
    }

    /// <summary>
    /// Resolves the data directory by walking parents from <see cref="AppContext.BaseDirectory"/> (the published output / exe folder).
    /// Use this for services and hosts so resolution matches CertTool and RefinerTool; do not use ASP.NET <c>ContentRootPath</c> alone,
    /// which points at the project directory under <c>dotnet run</c> and can pick a different <c>hephaestus_data</c> than tooling.
    /// </summary>
    public static string ResolveHephaestusDataRootFromAppBase(
        string dataDirectoryName = DefaultDataDirectoryName,
        int maxParentAscents = 50)
    {
        var start = Path.GetFullPath(AppContext.BaseDirectory);
        return ResolveHephaestusDataRoot(start, dataDirectoryName, maxParentAscents);
    }

    /// <param name="hephaestusDataRoot">Resolved <c>hephaestus_data</c> directory.</param>
    public static string WebDirectory(string hephaestusDataRoot, string webFolderName = "web")
    {
        var name = NormalizeSingleSegment(webFolderName, "web");
        return Path.GetFullPath(Path.Combine(hephaestusDataRoot, name));
    }

    /// <param name="hephaestusDataRoot">Resolved <c>hephaestus_data</c> directory.</param>
    public static string CertDirectory(string hephaestusDataRoot, string certFolderName = "cert")
    {
        var name = NormalizeSingleSegment(certFolderName, "cert");
        return Path.GetFullPath(Path.Combine(hephaestusDataRoot, name));
    }

    /// <summary>Domain catalog JSON at the root of <c>hephaestus_data</c> (not under <c>web</c>).</summary>
    public static string FileUnderDataRoot(string hephaestusDataRoot, string? fileName)
    {
        var file = string.IsNullOrWhiteSpace(fileName) ? "domains.json" : fileName.Trim();
        return Path.GetFullPath(Path.Combine(hephaestusDataRoot, file));
    }

    /// <param name="hephaestusDataRoot">Resolved <c>hephaestus_data</c> directory.</param>
    public static string FileUnderCert(string hephaestusDataRoot, string certFolderName, string fileName)
    {
        var cert = CertDirectory(hephaestusDataRoot, certFolderName);
        var file = string.IsNullOrWhiteSpace(fileName) ? "hephaestus.pfx" : fileName.Trim();
        return Path.GetFullPath(Path.Combine(cert, file));
    }

    #endregion

    private static string NormalizeDataDirectorySegment(string value)
    {
        var t = value.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (t.Length == 0)
            t = DefaultDataDirectoryName;
        if (t is "." or ".." || t.Contains(Path.DirectorySeparatorChar) || t.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException($"Invalid Hephaestus data directory name: '{value}'", nameof(value));
        return t;
    }

    private static string NormalizeSingleSegment(string value, string defaultIfEmpty)
    {
        var t = value.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (t.Length == 0)
            t = defaultIfEmpty;
        if (t is "." or ".." || t.Contains(Path.DirectorySeparatorChar) || t.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException($"Invalid path segment: '{value}'", nameof(value));
        return t;
    }
}
