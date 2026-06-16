using Microsoft.Extensions.Logging;

namespace Commons;

public sealed class ServerNetworkMaintenance : IServerNetworkMaintenance
{
    private readonly ServerService _serverService;
    private readonly ILogger<ServerNetworkMaintenance> _logger;

    public ServerNetworkMaintenance(ServerService serverService, ILogger<ServerNetworkMaintenance> logger)
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

            try
            {
                var server = _serverService.GetServerLite();
                _serverService.RefineCommonsAndSave(server);
                _logger.LogInformation(
                    "Server network refined: ServerIp={ServerIp}, PrimaryDns={PrimaryDns}, SecondaryDns={SecondaryDns}",
                    server.ServerIp,
                    server.PrimaryDns,
                    server.SecondaryDns);
            }
            catch (Exception ex)
            {
                _logger.LogErrorMessage(ex, "Server network refinement failed.");
            }
        }, cancellationToken);
    }
}
