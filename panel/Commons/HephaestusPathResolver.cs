using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Commons;

public sealed class HephaestusPathResolver : IHephaestusPathResolver
{
    public static string Profile = "default";

    private const string ProfileSubdirectoryName = "profile";
    private const string DefaultsSubdirectoryName = "defaults";

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

    public void EnsureDirectories(string startDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(startDirectory);
        Directory.CreateDirectory(ResolveHephaestusDataBase(startDirectory));
        Directory.CreateDirectory(ResolveHephaestusDataRoot(startDirectory));
        Directory.CreateDirectory(ProfileDirectory(startDirectory));
        Directory.CreateDirectory(WebDirectory(ResolveHephaestusDataRoot(startDirectory)));
        Directory.CreateDirectory(CertDirectory(repoRoot));
    }

    public void EnsureDirectoriesFromAppBase()
    {
        EnsureDirectories(Path.GetFullPath(AppContext.BaseDirectory));
    }

    public string ResolveRepositoryRoot(string startDirectory)
    {
        var relative = EffectiveRepositoryRootPath();
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(startDirectory), relative));
    }

    public string ResolveRepositoryRootFromAppBase()
    {
        return ResolveRepositoryRoot(Path.GetFullPath(AppContext.BaseDirectory));
    }

    public string ResolveHephaestusDataBase(string startDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(startDirectory);
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve Hephaestus data directory beside repository root '{repoRoot}': no parent directory.");
        var name = EffectiveHephaestusDataDirectoryName();
        var dataBase = Path.GetFullPath(Path.Combine(parent, name));
        EnsureOutsideRepository(repoRoot, dataBase);
        return dataBase;
    }

    public string ResolveHephaestusDataRoot(string startDirectory)
    {
        return Path.GetFullPath(Path.Combine(ResolveHephaestusDataBase(startDirectory), Profile));
    }

    public string ResolveHephaestusDataRootFromAppBase()
    {
        return ResolveHephaestusDataRoot(Path.GetFullPath(AppContext.BaseDirectory));
    }

    public string ProfileDirectory(string startDirectory)
    {
        return Path.GetFullPath(Path.Combine(ResolveHephaestusDataRoot(startDirectory), ProfileSubdirectoryName));
    }

    public string ProfileDirectoryFromAppBase()
    {
        return ProfileDirectory(Path.GetFullPath(AppContext.BaseDirectory));
    }

    public string DefaultsEmbedDirectory(string startDirectory)
    {
        return Path.GetFullPath(Path.Combine(ResolveHephaestusDataRoot(startDirectory), DefaultsSubdirectoryName));
    }

    public string WebDirectory(string profileRoot)
    {
        var name = EffectiveWebRootSegment();
        return Path.GetFullPath(Path.Combine(profileRoot, name));
    }

    public string CertDirectory(string repositoryRoot)
    {
        var name = EffectiveCertDirectorySegment();
        return Path.GetFullPath(Path.Combine(repositoryRoot, name));
    }

    public string FileUnderDataRoot(string profileRoot)
    {
        var file = EffectiveDomainsFileName();
        return Path.GetFullPath(Path.Combine(profileRoot, file));
    }

    public string DomainsIgnorePath(string repositoryRoot)
    {
        var file = EffectiveDomainsIgnoreFileName();
        return Path.GetFullPath(Path.Combine(repositoryRoot, file));
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

    private string EffectiveRepositoryRootPath() =>
        NormalizeRelativePath(_options.Value.RepositoryRoot, nameof(DomainHostOptions.RepositoryRoot));

    private string EffectiveHephaestusDataDirectoryName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.HephaestusData, nameof(DomainHostOptions.HephaestusData));

    private string EffectiveWebRootSegment() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.WebRoot, nameof(DomainHostOptions.WebRoot));

    private string EffectiveCertDirectorySegment() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.CertDirectoryName, nameof(DomainHostOptions.CertDirectoryName));

    private string EffectiveDomainsFileName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.DomainsFileName, nameof(DomainHostOptions.DomainsFileName));

    private string EffectiveDomainsIgnoreFileName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.DomainsIgnoreFileName, nameof(DomainHostOptions.DomainsIgnoreFileName));

    private string EffectiveCertPfxFileName() =>
        NormalizeSingleSegmentNotEmpty(_options.Value.CertPfxFileName, nameof(DomainHostOptions.CertPfxFileName));

    private static string NormalizeRelativePath(string value, string propertyName)
    {
        var t = value.Trim();
        if (t.Length == 0)
            throw new InvalidOperationException($"{propertyName} is empty.");
        if (Path.IsPathRooted(t))
            throw new ArgumentException($"{propertyName} must be a relative path, not absolute: '{value}'", nameof(value));
        return t;
    }

    private static void EnsureOutsideRepository(string repositoryRoot, string dataBase)
    {
        var repo = Path.GetFullPath(repositoryRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var data = Path.GetFullPath(dataBase);
        var repoPrefix = repo + Path.DirectorySeparatorChar;
        if (data.Equals(repo, StringComparison.OrdinalIgnoreCase)
            || data.StartsWith(repoPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hephaestus data directory '{data}' must be outside the repository root '{repositoryRoot}', not inside it.");
        }
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
