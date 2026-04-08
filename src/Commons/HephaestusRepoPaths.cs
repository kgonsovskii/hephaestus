namespace Commons;

public static class HephaestusRepoPaths
{
    public const string DefaultMarkerFileName = "defaulticon.ico";

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

    public static string WebDirectory(string repositoryRoot, string webFolderName = "web")
    {
        var name = NormalizeSingleSegment(webFolderName, "web");
        return Path.GetFullPath(Path.Combine(repositoryRoot, name));
    }

    public static string CertDirectory(string repositoryRoot, string certFolderName = "cert")
    {
        var name = NormalizeSingleSegment(certFolderName, "cert");
        return Path.GetFullPath(Path.Combine(repositoryRoot, name));
    }

    public static string FileUnderWeb(string repositoryRoot, string webFolderName, string fileName)
    {
        var web = WebDirectory(repositoryRoot, webFolderName);
        var file = string.IsNullOrWhiteSpace(fileName) ? "domains.json" : fileName.Trim();
        return Path.GetFullPath(Path.Combine(web, file));
    }

    public static string FileUnderCert(string repositoryRoot, string certFolderName, string fileName)
    {
        var cert = CertDirectory(repositoryRoot, certFolderName);
        var file = string.IsNullOrWhiteSpace(fileName) ? "hephaestus.pfx" : fileName.Trim();
        return Path.GetFullPath(Path.Combine(cert, file));
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
