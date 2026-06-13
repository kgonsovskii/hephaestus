using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Commons;

public sealed class HephaestusPathResolver : IHephaestusPathResolver
{
    private readonly IOptions<DomainHostOptions> _options;

    public HephaestusPathResolver(IOptions<DomainHostOptions> options)
    {
        _options = options;
    }

    public static HephaestusPathResolver FromSnapshot(DomainHostOptions snapshot)
    {
        DomainHostOptionsValidator.ValidateOrThrow(snapshot);
        return new HephaestusPathResolver(Options.Create(snapshot));
    }

    public static HephaestusPathResolver FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(DomainHostOptions.SectionName);
        if (!section.Exists() || !section.GetChildren().Any())
        {
            throw new InvalidOperationException(
                $"Configuration section '{DomainHostOptions.SectionName}' is missing or empty. Add it to appsettings.json (see Commons/appsettings.json).");
        }

        var o = section.Get<DomainHostOptions>()
            ?? throw new InvalidOperationException(
                $"Failed to bind configuration section '{DomainHostOptions.SectionName}'. Check property names for typos.");
        DomainHostOptionsValidator.ValidateOrThrow(o);
        return new HephaestusPathResolver(Options.Create(o));
    }

    /// <summary>
    /// Loads <c>DomainHost</c> from <c>appsettings.json</c> in <paramref name="baseDirectory"/> (required file and section).
    /// </summary>
    public static HephaestusPathResolver FromAppSettingsInDirectory(string baseDirectory)
    {
        var path = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Required file '{path}' was not found. Copy appsettings.json (including a '{DomainHostOptions.SectionName}' section) next to the application.");
        }

        var cfg = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        return FromConfiguration(cfg);
    }

    public string ResolveRepositoryRoot(string startDirectory)
    {
        var marker = EffectiveRepositoryMarkerFileName();
        var max = Math.Clamp(_options.Value.RepositoryRootSearchMaxAscents, 1, 200);
        return TryResolveRepositoryRoot(startDirectory, marker, max)
            ?? throw new InvalidOperationException(
                $"Could not find repository root: no directory within {max} ascents of '{Path.GetFullPath(startDirectory)}' contains marker file '{marker}'.");
    }

    public string ResolveHephaestusDataRoot(string startDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(startDirectory);
        var relative = EffectiveHephaestusDataPath();
        var dataRoot = Path.GetFullPath(Path.Combine(repoRoot, relative));
        if (!Directory.Exists(dataRoot))
        {
            throw new InvalidOperationException(
                $"Could not find Hephaestus data directory at '{dataRoot}' (repository root '{repoRoot}', {nameof(DomainHostOptions.HephaestusData)} '{relative}'). " +
                $"Create it beside the clone, e.g. ..\\hephaestus_data with web\\ and domains.json inside.");
        }

        return dataRoot;
    }

    public string ResolveHephaestusDataRootFromAppBase()
    {
        var start = Path.GetFullPath(AppContext.BaseDirectory);
        return ResolveHephaestusDataRoot(start);
    }

    public string WebDirectory(string hephaestusDataRoot)
    {
        var name = EffectiveWebRootSegment();
        return Path.GetFullPath(Path.Combine(hephaestusDataRoot, name));
    }

    public string CertDirectory(string repositoryRoot)
    {
        var name = EffectiveCertDirectorySegment();
        return Path.GetFullPath(Path.Combine(repositoryRoot, name));
    }

    public string FileUnderDataRoot(string hephaestusDataRoot)
    {
        var file = EffectiveDomainsFileName();
        return Path.GetFullPath(Path.Combine(hephaestusDataRoot, file));
    }

    public string FileUnderCert(string repositoryRoot)
    {
        var cert = CertDirectory(repositoryRoot);
        var file = EffectiveCertPfxFileName();
        return Path.GetFullPath(Path.Combine(cert, file));
    }

    public string PublicCertPath(string repositoryRoot)
    {
        var n = _options.Value.CertPublicCerFileName.Trim();
        if (n.Length == 0)
            throw new InvalidOperationException($"{nameof(DomainHostOptions.CertPublicCerFileName)} is empty.");
        return Path.GetFullPath(Path.Combine(CertDirectory(repositoryRoot), n));
    }

    private string EffectiveRepositoryMarkerFileName()
    {
        var m = _options.Value.RepositoryMarkerFileName.Trim();
        if (m.Length == 0)
            throw new InvalidOperationException($"{nameof(DomainHostOptions.RepositoryMarkerFileName)} is empty.");
        return m;
    }

    private string EffectiveHephaestusDataPath() =>
        NormalizeHephaestusDataPath(_options.Value.HephaestusData);

    private string EffectiveWebRootSegment() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.WebRoot, nameof(DomainHostOptions.WebRoot));

    private string EffectiveCertDirectorySegment() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.CertDirectoryName, nameof(DomainHostOptions.CertDirectoryName));

    private string EffectiveDomainsFileName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.DomainsFileName, nameof(DomainHostOptions.DomainsFileName));

    private string EffectiveCertPfxFileName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.CertPfxFileName, nameof(DomainHostOptions.CertPfxFileName));

    private static string? TryResolveRepositoryRoot(string startDirectory, string markerFileName, int maxParentAscents)
    {
        var marker = markerFileName.Trim();
        if (marker.Length == 0)
            return null;

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

    private static string NormalizeHephaestusDataPath(string value)
    {
        var t = value.Trim();
        if (t.Length == 0)
            throw new InvalidOperationException($"{nameof(DomainHostOptions.HephaestusData)} is empty.");
        if (Path.IsPathRooted(t))
            throw new ArgumentException($"HephaestusData must be relative to repository root, not an absolute path: '{value}'", nameof(value));
        return t;
    }

    private static string NormalizeSingleSegmentNotEmpty(string value, string propertyName)
    {
        var t = value.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (t.Length == 0)
            throw new InvalidOperationException($"{propertyName} is empty.");
        if (t is "." or ".." || t.Contains(Path.DirectorySeparatorChar) || t.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException($"Invalid path segment for {propertyName}: '{value}'", nameof(value));
        return t;
    }
}
