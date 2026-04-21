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
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            catch (Exception e)
            {
                ipAddress = "unknown";
            }

            if (Request.Headers.TryGetValue("HTTP_X_FORWARDED_FOR",
                    out Microsoft.Extensions.Primitives.StringValues value))
            {
                var forwardedFor = value.First();

                ipAddress = string.IsNullOrWhiteSpace(forwardedFor)
                    ? ipAddress
                    : forwardedFor.Split(',').Select(s => s.Trim()).FirstOrDefault();
            }

            return ipAddress;
        }
    }
}
