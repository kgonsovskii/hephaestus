using DomainHost.Models;
using Npgsql;

namespace DomainHost.Data;

public sealed class NpgsqlDomainRepository : IDomainRepository
{
    private readonly string _connectionString;

    public NpgsqlDomainRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
    }

    public async Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, enabled, domain, ip, domain_class, content_type, redirect_url
            FROM domains
            WHERE enabled = TRUE
            ORDER BY id
            """,
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var list = new List<DomainRecord>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var kind = ParseKind(reader.GetString(reader.GetOrdinal("content_type")));
            list.Add(new DomainRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Enabled = reader.GetBoolean(reader.GetOrdinal("enabled")),
                Domain = reader.GetString(reader.GetOrdinal("domain")),
                Ip = reader.IsDBNull(reader.GetOrdinal("ip")) ? null : reader.GetString(reader.GetOrdinal("ip")),
                DomainClass = reader.IsDBNull(reader.GetOrdinal("domain_class"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("domain_class")),
                ContentKind = kind,
                RedirectUrl = reader.IsDBNull(reader.GetOrdinal("redirect_url"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("redirect_url"))
            });
        }

        return list;
    }

    private static DomainContentKind ParseKind(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "html" => DomainContentKind.Html,
            "redirect" => DomainContentKind.Redirect,
            _ => DomainContentKind.JavaScript
        };
}
