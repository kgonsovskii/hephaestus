using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using model;

namespace Commons;

public sealed class HephaestusPathResolver : IHephaestusPathResolver
{
    public static string Profile = "default";

    static HephaestusPathResolver() =>
        ServerProfile.Current = () => Profile;

    private const string ProfileFileName = "profile.txt";
    private const string ServerSubdirectoryName = "server";
    private const string DefaultsSubdirectoryName = "defaults";
    private const string DomainsIgnoreFileName = "domains-ignore.json";
    private const string WebSitesFolderName = "sites";
    private const string WebClassesFolderName = "classes";
    private const string WebLayoutReadmeFileName = "readme.md";

    private const string WebRootReadme = """
        Static web content root for this Hephaestus profile.

        - `sites/` — per-hostname files (`sites/{domain}/`)
        - `classes/` — shared class fallbacks (`classes/{class}/`)

        DomainHost checks `sites/{hostname}/` first, then `classes/{class}/` (default class: `analytics`).
        """;

    private const string WebSitesReadme = """
        Per-domain static files. Add a folder named after the hostname (e.g. `example.com/`) with `index.html` or `index.js`.
        """;

    private const string WebClassesReadme = """
        Shared content classes. Each subfolder is a class name used when `sites/{hostname}/` has no match. Empty domain class uses `analytics`.
        """;

    private readonly IOptions<DomainHostOptions> _options;

    public HephaestusPathResolver(IOptions<DomainHostOptions> options)
    {
        _options = options;
    }

    public static HephaestusPathResolver FromSnapshot(DomainHostOptions snapshot)
    {
        DomainHostOptionsValidator.ValidateOrThrow(snapshot);
        ApplyProfileFromFileIfPresent(Path.GetFullPath(AppContext.BaseDirectory), snapshot);
        return new HephaestusPathResolver(Options.Create(snapshot));
    }

    public static HephaestusPathResolver FromConfiguration(IConfiguration configuration, string? startDirectory = null)
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
        ApplyProfileFromFileIfPresent(
            Path.GetFullPath(startDirectory ?? AppContext.BaseDirectory),
            o);
        return new HephaestusPathResolver(Options.Create(o));
    }

    /// <summary>
    /// If <c>{repositoryRoot}/../profile.txt</c> exists, sets <see cref="Profile"/> from its first line.
    /// </summary>
    public static bool ApplyProfileFromFileIfPresent(string startDirectory, DomainHostOptions options)
    {
        var profilePath = ResolveProfileFilePath(startDirectory, options);
        if (!File.Exists(profilePath))
            return false;

        var line = File.ReadLines(profilePath).FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(line))
            throw new InvalidOperationException($"Profile file '{profilePath}' is empty.");

        Profile = NormalizeProfileName(line);
        return true;
    }

    public static string ResolveProfileFilePath(string startDirectory, DomainHostOptions options)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            Path.GetFullPath(startDirectory),
            NormalizeRelativePath(options.RepositoryRoot, nameof(DomainHostOptions.RepositoryRoot))));
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve profile file beside repository root '{repoRoot}': no parent directory.");
        return Path.GetFullPath(Path.Combine(parent, ProfileFileName));
    }

    private static string NormalizeProfileName(string value) =>
        NormalizeSingleSegmentNotEmpty(value, "profile.txt");

    public static string ValidateProfileName(string value) => NormalizeProfileName(value);

    public static void WriteProfileFile(string repositoryRoot, string profileName)
    {
        var profile = ValidateProfileName(profileName);
        var path = ResolveProfileFilePathFromRepoRoot(repositoryRoot);
        File.WriteAllText(path, profile + Environment.NewLine);
        Profile = profile;
    }

    public static string ResolveProfileFilePathFromRepoRoot(string repositoryRoot)
    {
        var repoRoot = Path.GetFullPath(repositoryRoot);
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve profile file beside repository root '{repoRoot}': no parent directory.");
        return Path.GetFullPath(Path.Combine(parent, ProfileFileName));
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
        return FromConfiguration(cfg, Path.GetFullPath(baseDirectory));
    }

    public void EnsureDirectories(string startDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(startDirectory);
        Directory.CreateDirectory(ResolveHephaestusDataBase(startDirectory));
        Directory.CreateDirectory(ResolveHephaestusDataRoot(startDirectory));
        Directory.CreateDirectory(ServerDirectory(startDirectory));
        EnsureWebLayout(WebDirectory(ResolveHephaestusDataRoot(startDirectory)));
        Directory.CreateDirectory(CertDirectory(repoRoot));
    }

    private static void EnsureWebLayout(string webRoot)
    {
        Directory.CreateDirectory(webRoot);
        var sitesDir = Path.Combine(webRoot, WebSitesFolderName);
        var classesDir = Path.Combine(webRoot, WebClassesFolderName);
        Directory.CreateDirectory(sitesDir);
        Directory.CreateDirectory(classesDir);
        TryWriteClearFolderReadme(webRoot, WebRootReadme, webRootLayout: true);
        TryWriteClearFolderReadme(sitesDir, WebSitesReadme, webRootLayout: false);
        TryWriteClearFolderReadme(classesDir, WebClassesReadme, webRootLayout: false);
    }

    private static void TryWriteClearFolderReadme(string directory, string content, bool webRootLayout)
    {
        if (!IsClearForReadme(directory, webRootLayout))
            return;

        var path = Path.Combine(directory, WebLayoutReadmeFileName);
        if (File.Exists(path))
            return;

        File.WriteAllText(path, content.TrimEnd() + Environment.NewLine);
    }

    private static bool IsClearForReadme(string directory, bool webRootLayout)
    {
        if (!Directory.Exists(directory))
            return false;

        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var name = Path.GetFileName(entry);
            if (string.Equals(name, WebLayoutReadmeFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (webRootLayout && Directory.Exists(entry)
                && (string.Equals(name, WebSitesFolderName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, WebClassesFolderName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return false;
        }

        return true;
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

    public string ServerDirectory(string startDirectory)
    {
        return Path.GetFullPath(Path.Combine(ResolveHephaestusDataRoot(startDirectory), ServerSubdirectoryName));
    }

    public string ServerDirectoryFromAppBase()
    {
        return ServerDirectory(Path.GetFullPath(AppContext.BaseDirectory));
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

    public string DomainsIgnorePath(string startDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(startDirectory);
        return Path.GetFullPath(Path.Combine(repoRoot, DomainsIgnoreFileName));
    }

    public string DomainsIgnorePathFromAppBase()
    {
        return DomainsIgnorePath(Path.GetFullPath(AppContext.BaseDirectory));
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
