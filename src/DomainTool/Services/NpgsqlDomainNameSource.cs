using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DomainTool.Services;

public sealed class NpgsqlDomainNameSource : IDomainNameSource
{
    private readonly string _connectionString;

    public NpgsqlDomainNameSource(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
    }

    public async Task<IReadOnlyList<string>> GetEnabledDomainNamesAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            """
            SELECT domain
            FROM domains
            WHERE enabled = TRUE
            ORDER BY domain
            """,
            conn);

        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(reader.GetString(0));

        return list;
    }
}
