using System.Text.Json;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Domain;

public interface IDomainRepository
{
    Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken);
}

public sealed class JsonFileDomainRepository : IDomainRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebContentPathProvider _webPaths;
    private readonly string _domainsFileName;

    public JsonFileDomainRepository(IWebContentPathProvider webPaths, IOptions<DomainHostOptions> options)
    {
        _webPaths = webPaths;
        var name = options.Value.DomainsFileName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _domainsFileName = name.Length == 0 ? "domains.json" : name;
    }

    public async Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_webPaths.WebRootFullPath, _domainsFileName);
        await using var stream = File.OpenRead(path);
        var doc = await JsonSerializer.DeserializeAsync<DomainsFileDto>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (doc is null)
            throw new InvalidOperationException($"Invalid domains file: {path}");

        var list = new List<DomainRecord>();
        foreach (var row in doc.Domains ?? [])
        {
            if (!row.Enabled)
                continue;
            list.Add(new DomainRecord
            {
                Enabled = row.Enabled,
                Domain = row.Domain ?? "",
                Ip = row.Ip,
                DomainClass = row.DomainClass ?? "",
                ContentKind = ParseKind(row.ContentType),
                RedirectUrl = row.RedirectUrl
            });
        }

        list.Sort((a, b) => string.Compare(a.Domain, b.Domain, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    private static DomainContentKind ParseKind(string? raw) =>
        (raw ?? "").ToLowerInvariant() switch
        {
            "html" => DomainContentKind.Html,
            "redirect" => DomainContentKind.Redirect,
            _ => DomainContentKind.JavaScript
        };

    private sealed class DomainsFileDto
    {
        public List<DomainsFileRowDto>? Domains { get; set; }
    }

    private sealed class DomainsFileRowDto
    {
        public bool Enabled { get; set; }

        public string? Domain { get; set; }

        public string? Ip { get; set; }

        public string? DomainClass { get; set; }

        public string? ContentType { get; set; }

        public string? RedirectUrl { get; set; }
    }
}
