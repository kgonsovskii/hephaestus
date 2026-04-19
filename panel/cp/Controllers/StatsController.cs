using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;
using Npgsql;

namespace cp.Controllers;

[Route("[controller]")]
public class StatsController: BaseController
{   
    public StatsController(ServerService serverService,IConfiguration configuration, IMemoryCache memoryCache): base(serverService, configuration, memoryCache)
    {
   
    }
    
    internal void ClearStats()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using var command = new NpgsqlCommand("TRUNCATE TABLE bot_log, dn_log", connection);
            command.ExecuteNonQuery();
        }
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet("dayly")]
    public async Task<IActionResult> ViewStats()
    {
        var server = Server;
        var stats = new List<DailyServerSerieStats>();

        try
        {
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(
                    """
                    SELECT
                      stat_date AS "Date",
                      server AS "server",
                      serie AS "Serie",
                      unique_id_count AS "UniqueIDCount",
                      elevated_unique_id_count AS "ElevatedUniqueIDCount",
                      number_of_downloads AS "NumberOfDownloads",
                      install_count AS "InstallCount",
                      uninstall_count AS "UnInstallCount"
                    FROM daily_server_serie_stats_view
                    WHERE server = @server
                    ORDER BY stat_date DESC
                    LIMIT 1000
                    """, connection))
                {
                    command.Parameters.AddWithValue("server", server);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stat = new DailyServerSerieStats
                            {
                                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                                Server = reader.GetString(reader.GetOrdinal("server")),
                                Serie = reader.GetString(reader.GetOrdinal("Serie")),
                                UniqueIDCount = reader.GetInt32(reader.GetOrdinal("UniqueIDCount")),
                                ElevatedUniqueIDCount = reader.GetInt32(reader.GetOrdinal("ElevatedUniqueIDCount")),
                                NumberOfDownloads = (int)reader.GetInt64(reader.GetOrdinal("NumberOfDownloads")),
                                InstallCount = reader.GetInt32(reader.GetOrdinal("InstallCount")),
                                UnInstallCount = reader.GetInt32(reader.GetOrdinal("UnInstallCount"))
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
        return View("Dayly", stats);
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet("botlog")]
    public async Task<IActionResult> BotLog()
    {
        var server = Server;
        var stats = new List<BotLog>();

        try
        {
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(
                    """
                    SELECT
                      id,
                      server,
                      first_seen,
                      last_seen,
                      first_seen_ip,
                      last_seen_ip,
                      serie,
                      number_of_requests,
                      number_of_elevated_requests,
                      number_of_downloads
                    FROM bot_log_view
                    WHERE server = @server
                    ORDER BY last_seen DESC
                    LIMIT 1000
                    """, connection))
                {
                    command.Parameters.AddWithValue("server", server);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stat = new BotLog()
                            {
                                Id = reader.GetString(reader.GetOrdinal("id")),
                                Server = reader.GetString(reader.GetOrdinal("server")),
                                LastSeen = reader.GetDateTime(reader.GetOrdinal("last_seen")),
                                LastSeenIp = reader.GetString(reader.GetOrdinal("last_seen_ip")),
                                FirstSeen = reader.GetDateTime(reader.GetOrdinal("first_seen")),
                                FirstSeenIp = reader.GetString(reader.GetOrdinal("first_seen_ip")),
                                Serie = reader.GetString(reader.GetOrdinal("serie")),
                                NumberOfRequests = reader.GetInt32(reader.GetOrdinal("number_of_requests")),
                                NumberOfElevatedRequests = reader.GetInt32(reader.GetOrdinal("number_of_elevated_requests")),
                                NumberOfDownloads = (int)reader.GetInt64(reader.GetOrdinal("number_of_downloads"))
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

        return View("BotLog", stats);
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet("downloadlog")]
    public async Task<IActionResult> DownloadLog()
    {
        var server = Server;
        var stats = new List<DownloadLog>();

        try
        {
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(
                    """
                    SELECT
                      ip,
                      server,
                      profile,
                      first_seen,
                      last_seen,
                      number_of_requests
                    FROM download_log_view
                    WHERE server = @server
                    ORDER BY last_seen DESC
                    LIMIT 1000
                    """, connection))
                {
                    command.Parameters.AddWithValue("server", server);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stat = new DownloadLog()
                            {
                                Ip = reader.GetString(reader.GetOrdinal("ip")),
                                Server = reader.GetString(reader.GetOrdinal("server")),
                                Profile = reader.GetString(reader.GetOrdinal("profile")),
                                FirstSeen = reader.GetDateTime(reader.GetOrdinal("first_seen")),
                                LastSeen = reader.GetDateTime(reader.GetOrdinal("last_seen")),
                                NumberOfRequests = reader.GetInt32(reader.GetOrdinal("number_of_requests")),
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

        return View("DownloadLog", stats);
    }
}
