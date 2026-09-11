using System.Text.Json;
using Commons;
using cp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

public abstract class BaseController: Controller
{
    protected readonly ServerService _serverService;

    protected readonly IMemoryCache _memoryCache;

    protected string RootDataDir => _serverService.Paths.RootData;

    protected const string SecretKey = "YourSecretKeyHere"; 
    
    protected readonly string _connectionString;
        
    protected static JsonSerializerOptions JsonOptions => BotUpsertSigning.UpsertJsonOptions;
    
    protected async Task<IActionResult> GetFileX(string file, string name, string type)
    {
        try
        {
            

            if (type == "vbs")
            {
                Response.Headers.Add("Content-Type", "text/plain");
            }
            else
            {
                Response.Headers.Add("Content-Type", "application/octet-stream");
            }

            var fileBytes = System.IO.File.ReadAllBytes(file);
            return File(fileBytes, "application/octet-stream", name + "." + type);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    protected BaseController(ServerService serverService,IConfiguration configuration, IMemoryCache memoryCache)
    {
        _serverService = serverService;
        _memoryCache = memoryCache;
        _connectionString = configuration.GetConnectionString("Default");
    }
    
    protected string Server
    {
        get
        {
            return BackSvc.EvalServer(Request);
        }
    }

    protected string IpAddress
    {
        get
        {
            string ipAddress = "unknown";
            try
            {
                var remote = HttpContext.Connection.RemoteIpAddress;
                if (remote is not null && !ClientIpNormalizer.IsPrivate(remote))
                    ipAddress = ClientIpNormalizer.Normalize(remote.ToString());
                else
                    ipAddress = ClientIpNormalizer.Normalize(remote?.ToString()) is { Length: > 0 } n ? n : "unknown";
            }
            catch (Exception)
            {
                ipAddress = "unknown";
            }

            var remoteIsPrivate = ipAddress == "unknown" ||
                (System.Net.IPAddress.TryParse(ipAddress, out var parsed) && ClientIpNormalizer.IsPrivate(parsed));
            if (remoteIsPrivate &&
                (Request.Headers.TryGetValue("X-Forwarded-For", out var value) ||
                 Request.Headers.TryGetValue("HTTP_X_FORWARDED_FOR", out value)))
            {
                var forwarded = ClientIpNormalizer.Normalize(value.ToString());
                if (forwarded.Length > 0)
                    ipAddress = forwarded;
            }

            return string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress;
        }
    }
}
