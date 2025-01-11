using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("[controller]")]
public class StatsController: BaseController
{   
    public StatsController(ServerService serverService,IConfiguration configuration, IMemoryCache memoryCache): base(serverService, configuration, memoryCache)
    {
   
    }
    
    internal void ClearStats()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            var command = new SqlCommand( "truncate table dbo.botLog; truncate table dbo.dnLog");
            command.CommandType = CommandType.Text;
            command.Connection = connection;
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
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new SqlCommand($"SELECT TOP (1000) [Date], [server], [Serie], [UniqueIDCount], [ElevatedUniqueIDCount],NumberOfDownloads,InstallCount,UnInstallCount FROM [hephaestus].[dbo].[DailyServerSerieStatsView] where server = '{server}' order by date desc", connection))
                {
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
                                NumberOfDownloads = reader.GetInt32(reader.GetOrdinal("NumberOfDownloads")),
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
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new SqlCommand($@"SELECT TOP (1000) [id]
      ,[server]
      ,[first_seen]
      ,[last_seen]
      ,[first_seen_ip]
      ,[last_seen_ip]
      ,[serie]
      ,[number]
      ,[number_of_requests]
      ,[number_of_elevated_requests]
      ,[number_of_downloads]
  FROM [hephaestus].[dbo].[BotLogView]
  where server='{server}' order by last_seen desc", connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stat = new BotLog()
                            {
                                Id = reader.GetString("id"),
                                Server = reader.GetString(reader.GetOrdinal("server")),
                                LastSeen = reader.GetDateTime(reader.GetOrdinal("last_seen")),
                                LastSeenIp = reader.GetString(reader.GetOrdinal("last_seen_ip")),
                                FirstSeen = reader.GetDateTime(reader.GetOrdinal("first_seen")),
                                FirstSeenIp = reader.GetString(reader.GetOrdinal("first_seen_ip")),
                                Serie = reader.GetString("serie"),
                                Number = reader.GetString("number"),
                                NumberOfRequests =  reader.GetOrdinal("number_of_requests"),
                                NumberOfElevatedRequests =  reader.GetInt32("number_of_elevated_requests"),
                                NumberOfDownloads =  reader.GetInt32("number_of_downloads")
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (ex) here
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
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new SqlCommand($@"SELECT TOP (1000) 
        [ip]
      ,[server]
      ,[profile]
      ,[first_seen]
      ,[last_seen]
      ,[number_of_requests]
  FROM [hephaestus].[dbo].[DownloadLogView]
  where server='{server}' order by last_seen desc", connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stat = new DownloadLog()
                            {
                                Ip = reader.GetString("ip"),
                                Server = reader.GetString(reader.GetOrdinal("server")),
                                Profile = reader.GetString(reader.GetOrdinal("profile")),
                                FirstSeen = reader.GetDateTime(reader.GetOrdinal("first_seen")),
                                LastSeen = reader.GetDateTime(reader.GetOrdinal("last_seen")),
                                NumberOfRequests =  reader.GetOrdinal("number_of_requests"),
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (ex) here
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

        return View("DownloadLog", stats);
    }
}