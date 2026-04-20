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
    public async Task<IActionResult> CloneServer([FromBody] CloneStartBody body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.CloneServerIp))
            return BadRequest(new { error = "cloneServerIp is required." });

        if (string.IsNullOrWhiteSpace(body.CloneUser))
            return BadRequest(new { error = "cloneUser is required." });

        if (CloneRemoteInstallTarget.ValidateHost(body.CloneServerIp) is { } hostErr)
            return BadRequest(new { error = hostErr });

        var runId = await _remoteInstall
            .StartRemoteInstallAsync(body.CloneServerIp, body.CloneUser, body.ClonePassword ?? "", cancellationToken)
            .ConfigureAwait(false);

        return Json(new { runId });
    }

    [HttpPost("clone/schedule-update")]
    public IActionResult ScheduleUpdate()
    {
        if (!OperatingSystem.IsLinux())
            return StatusCode(503, new { error = "Update is only supported when the panel runs on Linux." });

        var repoRoot = RepoRootResolver.Resolve(_clonerOptions.CurrentValue.RepoRoot, _logger);
        var workDir = Path.Combine(repoRoot, "install");
        var updateScript = Path.Combine(workDir, "update.sh");
        if (!System.IO.File.Exists(updateScript))
            return StatusCode(500, new { error = $"Missing script: {updateScript}" });

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("nohup /bin/bash ./update.sh </dev/null >/dev/null 2>&1 &");

        using var proc = Process.Start(psi);
        if (proc is null)
            return StatusCode(500, new { error = "Failed to start update.sh" });

        proc.WaitForExit();
        if (proc.ExitCode != 0)
            return StatusCode(500, new { error = $"Failed to queue update.sh (exit {proc.ExitCode})" });

        _logger.LogInformation("update: nohup update.sh queued from {WorkDir}", workDir);
        return Json(new
        {
            ok = true,
            message = "OK: update.sh started in background (no reboot). Service will restart when install finishes.",
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
