using Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Db;

public interface IStatsMaintenance : IMaintenance
{
}

public sealed class StatsMaintenance : IStatsMaintenance
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatsMaintenance> _logger;

    public StatsMaintenance(IConfiguration configuration, ILogger<StatsMaintenance> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cs = _configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("Stats maintenance skipped: ConnectionStrings:Default is not configured");
            return;
        }

        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var stats = new NpgsqlCommand("SELECT calc_stats()", connection))
            await stats.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using (var clean = new NpgsqlCommand("SELECT clean()", connection))
            await clean.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Stats maintenance completed (calc_stats, clean)");
    }
}
