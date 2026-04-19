using Cloner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

public class CloneController : BaseController
{
    private readonly IClonerRemoteInstall _remoteInstall;

    public CloneController(
        ServerService serverService,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IClonerRemoteInstall remoteInstall) : base(serverService, configuration, memoryCache)
    {
        _remoteInstall = remoteInstall;
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

    [HttpPost("clone/stop")]
    public IActionResult Stop([FromBody] CloneStopRequest? body)
    {
        if (body == null || body.RunId == Guid.Empty)
            return BadRequest(new { error = "runId required" });

        var stopped = _remoteInstall.TryStop(body.RunId);
        return Json(new { stopped });
    }
}
