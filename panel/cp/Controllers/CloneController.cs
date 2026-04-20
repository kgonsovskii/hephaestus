using System.Diagnostics;
using Cloner;
using Commons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using model;

namespace cp.Controllers;

public class CloneController : BaseController
{
    private readonly IClonerRemoteInstall _remoteInstall;
    private readonly IOptionsMonitor<ClonerOptions> _clonerOptions;
    private readonly ILogger<CloneController> _logger;

    public CloneController(
        ServerService serverService,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IClonerRemoteInstall remoteInstall,
        IOptionsMonitor<ClonerOptions> clonerOptions,
        ILogger<CloneController> logger) : base(serverService, configuration, memoryCache)
    {
        _remoteInstall = remoteInstall;
        _clonerOptions = clonerOptions;
        _logger = logger;
    }

    [HttpGet("clone")]
    public IActionResult Index()
    {
        return View("~/Views/Clone/Index.cshtml", new CloneModel());
    }

    [HttpPost("clone")]
    public async Task<IActionResult> CloneServer([FromBody] CloneModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid model" });

        if (CloneRemoteInstallTarget.ValidateHost(model.CloneServerIp) is { } hostErr)
            return BadRequest(new { error = hostErr });

        var runId = await _remoteInstall
            .StartRemoteInstallAsync(model.CloneServerIp, model.CloneUser, model.ClonePassword, cancellationToken)
            .ConfigureAwait(false);

        return Json(new { runId });
    }

    [HttpPost("clone/schedule-update")]
    public async Task<IActionResult> ScheduleUpdate(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux())
            return StatusCode(503, new { error = "Schedule update is only supported on Linux." });

        var repoRoot = RepoRootResolver.Resolve(_clonerOptions.CurrentValue.RepoRoot, _logger);
        var scriptPath = Path.Combine(repoRoot, "install", "schedule_update.sh");
        if (!System.IO.File.Exists(scriptPath))
            return StatusCode(500, new { error = $"Missing script: {scriptPath}" });

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            WorkingDirectory = Path.Combine(repoRoot, "install"),
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(scriptPath);

        using var proc = Process.Start(psi);
        if (proc is null)
            return StatusCode(500, new { error = "Failed to start schedule_update.sh" });

        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (proc.ExitCode != 0)
            return StatusCode(500, new { error = $"schedule_update.sh exited with code {proc.ExitCode}" });

        _logger.LogInformation("schedule-update: schedule_update.sh exited 0; update runs in background");
        return Json(new
        {
            ok = true,
            message = "OK: update started in background. Allow up to about 5 minutes for git pull and reinstall.",
        });
    }

    [HttpPost("clone/stop")]
    public IActionResult Stop([FromBody] CloneStopRequest? body)
    {
        if (body == null || body.RunId == Guid.Empty)
            return BadRequest(new { error = "runId required" });

        var stopped = _remoteInstall.TryStop(body.RunId);
        return Json(new { stopped });
    }
}
