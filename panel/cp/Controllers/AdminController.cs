using cp.Code;
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

    public Dictionary<string, string> AdminServers()
    {
        var result = new Dictionary<string, string>();
        var dir = Path.Combine(RootDataDir, PanelServerIdentity.DefaultKey);
        if (Directory.Exists(dir))
            result.Add(dir, PanelServerIdentity.DefaultKey);
        return result;
    }
    
    [HttpGet] [Route("/admin")]
    public IActionResult IndexAdmin()
    {
        return View("admin", new ServerModel(){AdminServers = AdminServers()});
    }

    [HttpPost] [Route("/admin")]
    private IActionResult IndexAdmin(ServerModel updatedModel)
    {
        if (updatedModel.AdminPassword != Environment.GetEnvironmentVariable("SuperPassword", EnvironmentVariableTarget.Machine))
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
        
        foreach (var _ in toAdd)
        {
            _serverService.GetServerHard();
        }
        
        return IndexAdmin();
    }
}