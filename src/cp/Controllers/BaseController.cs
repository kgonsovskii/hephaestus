using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

public abstract class BaseController: Controller
{
    protected readonly ServerService _serverService;

    protected readonly IMemoryCache _memoryCache;
    
    protected static string RootDataDir => ServerModelLoader.RootDataStatic;

    protected const string SecretKey = "YourSecretKeyHere"; // Secret key for hashing
    
    protected readonly string _connectionString;
        
    protected static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false // Ensure compact JSON
    };
    
    protected async Task<IActionResult> GetFileX(string file, string name, string type)
    {
        try
        {
            /*string fileContent;
            if (!_memoryCache.TryGetValue(file, out fileContent))
            {
                fileContent = await System.IO.File.ReadAllTextAsync(file);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1));
                _memoryCache.Set(file, fileContent, cacheEntryOptions);
            }*/

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