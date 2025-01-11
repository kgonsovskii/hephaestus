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