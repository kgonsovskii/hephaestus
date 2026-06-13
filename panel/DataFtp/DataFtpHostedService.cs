using FubarDev.FtpServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataFtp;

internal sealed class DataFtpHostedService : IHostedService
{
    private readonly IFtpServerHost _ftpServerHost;
    private readonly ILogger<DataFtpHostedService> _logger;

    public DataFtpHostedService(IFtpServerHost ftpServerHost, ILogger<DataFtpHostedService> logger)
    {
        _ftpServerHost = ftpServerHost;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _ftpServerHost.StartAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Data FTP server started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data FTP server failed to start.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _ftpServerHost.StopAsync(cancellationToken);
}
