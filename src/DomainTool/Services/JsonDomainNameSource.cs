using System.Text.Json;
using Commons;
using DomainTool.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DomainTool.Services;

public sealed class JsonDomainNameSource : IDomainNameSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHostEnvironment _env;
    private readonly DomainToolOptions _options;

    public JsonDomainNameSource(IHostEnvironment env, IOptions<DomainToolOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> GetEnabledDomainNamesAsync(CancellationToken cancellationToken = default)
    {
        var webRoot = ResolveWebRootFullPath();
        var fileName = string.IsNullOrWhiteSpace(_options.DomainsFileName)
            ? "domains.json"
            : _options.DomainsFileName.Trim();
        var path = Path.Combine(webRoot, fileName);
        await using var stream = File.OpenRead(path);
        var doc = await JsonSerializer.DeserializeAsync<DomainsFileDto>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (doc is null)
            throw new InvalidOperationException($"Invalid domains file: {path}");

        var names = new List<string>();
        foreach (var row in doc.Domains ?? [])
        {
            if (row.Enabled && !string.IsNullOrWhiteSpace(row.Domain))
                names.Add(row.Domain.Trim());
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    private string ResolveWebRootFullPath()
    {
        var folderName = _options.WebRoot.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (folderName.Length == 0)
            folderName = "web";
        var maxSteps = Math.Clamp(_options.WebRootSearchMaxAscents, 1, 200);
        var start = Path.GetFullPath(_env.ContentRootPath);
        var repoRoot = HephaestusRepoPaths.ResolveRepositoryRoot(start, HephaestusRepoPaths.DefaultMarkerFileName, maxSteps);
        var webFull = HephaestusRepoPaths.WebDirectory(repoRoot, folderName);
        if (!Directory.Exists(webFull))
            throw new InvalidOperationException(
                $"DomainTool: web directory not found at '{webFull}' (repository root '{repoRoot}').");

        return webFull;
    }

    private sealed class DomainsFileDto
    {
        public List<DomainsFileRowDto>? Domains { get; set; }
    }

    private sealed class DomainsFileRowDto
    {
        public bool Enabled { get; set; }

        public string? Domain { get; set; }
    }
}
