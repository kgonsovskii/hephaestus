using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("manage")]
public class ManageController : BaseController
{
    public ManageController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache): base(serverService, configuration, memoryCache)
    {
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet]
    public IActionResult Index(string name)
    {
        var server = Server;
        if (server == "favicon.ico")
            return NotFound();
        try
        {
            var serverResult = _serverService.GetServer(server, false);
            var model = serverResult.ServerModel.DomainIps.First(a=> a.Name == name);
            return View("Index", model);
        }
        catch (Exception e)
        {
            return View("Index", new DomainIp() {Name = name, Result = e.Message + "\r\n" + e.StackTrace });
        }
    }
  
    [HttpPost]
    [Authorize(Policy = "AllowFromIpRange")]
    public IActionResult IndexWithServer(DomainIp updatedModel)
    {
        var server = Server;
        try
        {
            var existingModel = _serverService.GetServer(server, true).ServerModel;
            if (existingModel == null)
            {
                return NotFound();
            }

            updatedModel.Domains = updatedModel.Domains
                .Where(a => !string.IsNullOrEmpty(a))
                .SelectMany(a => a.Split(Environment.NewLine))
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).Distinct().ToList();

            var exi = existingModel.DomainIps.First(a => a.Index == updatedModel.Index);
            exi.Domains.Clear();

            var alldomains = existingModel.DomainIps.SelectMany(a => a.Domains).Distinct().ToList();
            if (alldomains.Any(a => updatedModel.Domains.Contains(a)))
            {
                updatedModel.Result = "Домены не уникальны глобально";
                return View("Index", updatedModel);
            }
            
            exi.Assign(updatedModel, true);

            var result = _serverService.PostServer(server, existingModel, "apply", "kill");
            var model = existingModel.DomainIps.First(a=> a.Name == updatedModel.Name);
            existingModel.Result = result;
            return View("Index", model);
        }
        catch (Exception e)
        {
            return View("Index", new DomainIp() {Name = updatedModel.Name, Result = e.Message + "\r\n" + e.StackTrace });
        }
    }
}