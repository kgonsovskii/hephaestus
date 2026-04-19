using System.Text.Json;
using Commons;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Domain;

public interface IDomainRepository
{
    Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<DomainRecord>> LoadAllDomainsAsync(CancellationToken cancellationToken = default);

        Task SaveDomainsAsync(IReadOnlyList<DomainRecord> domains, CancellationToken cancellationToken = default);
}

public sealed class JsonFileDomainRepository : IDomainRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IWebContentPathProvider _webPaths;
    private readonly string _domainsFileName;

    public JsonFileDomainRepository(IWebContentPathProvider webPaths, IOptions<DomainHostOptions> options)
    {
        _webPaths = webPaths;
        _domainsFileName = options.Value.DomainsFileName.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public async Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_webPaths.DataRootFullPath, _domainsFileName);
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
            list.Add(MapRow(row));
        }

        list.Sort((a, b) => string.Compare(a.Domain, b.Domain, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public async Task<IReadOnlyList<DomainRecord>> LoadAllDomainsAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_webPaths.DataRootFullPath, _domainsFileName);
        if (!File.Exists(path))
            return Array.Empty<DomainRecord>();

        await using var stream = File.OpenRead(path);
        var doc = await JsonSerializer.DeserializeAsync<DomainsFileDto>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (doc is null)
            throw new InvalidOperationException($"Invalid domains file: {path}");

        var list = new List<DomainRecord>();
        foreach (var row in doc.Domains ?? [])
            list.Add(MapRow(row));

        return list;
    }

    public async Task SaveDomainsAsync(IReadOnlyList<DomainRecord> domains, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_webPaths.DataRootFullPath, _domainsFileName);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var rows = new List<DomainsFileRowDto>();
        foreach (var d in domains)
        {
            rows.Add(new DomainsFileRowDto
            {
                Enabled = d.Enabled,
                Domain = d.Domain,
                Ip = d.Ip,
                DomainClass = d.DomainClass,
                ContentType = KindToContentType(d.ContentKind),
                RedirectUrl = d.RedirectUrl
            });
        }

        var dto = new DomainsFileDto { Domains = rows };
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, dto, WriteOptions, cancellationToken).ConfigureAwait(false);
    }

    private static string KindToContentType(DomainContentKind kind) =>
        kind switch
        {
            DomainContentKind.Html => "html",
            DomainContentKind.Redirect => "redirect",
            _ => "javascript"
        };

    private static DomainRecord MapRow(DomainsFileRowDto row) =>
        new()
        {
            Enabled = row.Enabled,
            Domain = row.Domain ?? "",
            Ip = row.Ip,
            DomainClass = row.DomainClass ?? "",
            ContentKind = ParseKind(row.ContentType),
            RedirectUrl = row.RedirectUrl
        };

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
