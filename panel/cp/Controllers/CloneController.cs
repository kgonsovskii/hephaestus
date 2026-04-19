using Cloner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Authorize(Policy = "AllowFromIpRange")]
[Route("[controller]")]
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

    public IActionResult Index()
    {
        return View("Components/Clone/Default", new CloneModel());
    }

    [HttpPost("clone")]
    public async Task<IActionResult> CloneServer([FromBody] CloneModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid model" });

        var runId = await _remoteInstall
            .StartRemoteInstallAsync(model.CloneServerIp, model.CloneUser, model.ClonePassword, cancellationToken)
            .ConfigureAwait(false);

        return Json(new { runId });
    }
}
