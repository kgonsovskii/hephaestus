using cp.Code;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("[controller]")]
public class AdminController: BaseController
{
    public AdminController(ServerService serverService,IConfiguration configuration, IMemoryCache memoryCache): base(serverService, configuration, memoryCache)
    {
    }

    public static Dictionary<string, string> AdminServers()
    {
        var result = new Dictionary<string, string>();
        var dirs = Directory.GetDirectories(RootDataDir).ToArray();
        foreach (var dir in dirs)
        {
            var password = "password";
            result.Add(dir, Path.GetFileName(dir));
        }
        return result;
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet] [Route("/admin")]
    public IActionResult IndexAdmin()
    {
        return View("admin", new ServerModel(){AdminServers = AdminServers()});
    }

    [Authorize(Policy = "AllowFromIpRange")]
    [HttpPost] [Route("/admin")]
    private IActionResult IndexAdmin(ServerModel updatedModel)
    {
        if (updatedModel.AdminPassword != System.Environment.GetEnvironmentVariable("SuperPassword", EnvironmentVariableTarget.Machine))
        {
            return Unauthorized();
        }

        var was = AdminServers();

        var toDelete = was.Where(a => !updatedModel.AdminServers.ContainsKey(a.Key));
        
        var toAdd = updatedModel.AdminServers.Where(a => !was.ContainsKey(a.Key));


        
        foreach (var server in toDelete)
        {
            ServerUtils.DeleteFolderRecursive(server.Key);
        }
        
        foreach (var server in toAdd)
        {
            _serverService.GetServer(server.Key, false, true, server.Value);
        }
        
        return IndexAdmin();
    }
}