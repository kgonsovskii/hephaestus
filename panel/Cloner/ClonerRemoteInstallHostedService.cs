using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cloner;

public sealed class ClonerRemoteInstallHostedService : BackgroundService
{
    private readonly ClonerRemoteInstallService _coordinator;
    private readonly IOptionsMonitor<ClonerOptions> _options;
    private readonly ILogger<ClonerRemoteInstallHostedService> _logger;

    public ClonerRemoteInstallHostedService(
        ClonerRemoteInstallService coordinator,
        IOptionsMonitor<ClonerOptions> options,
        ILogger<ClonerRemoteInstallHostedService> logger)
    {
        _coordinator = coordinator;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _coordinator.JobReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunInstallProcessAsync(job, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloner: install failed for {RunId}", job.RunId);
                await job.LogWriter.WriteAsync($"[error] {ex.Message}", stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                job.LogWriter.TryComplete();
                _coordinator.CompleteRun(job.RunId);
            }
        }
    }

    private async Task RunInstallProcessAsync(RemoteInstallJob job, CancellationToken stoppingToken)
    {
        var repoRoot = RepoRootResolver.Resolve(_options.CurrentValue.RepoRoot, _logger);
        var installDir = Path.Combine(repoRoot, "install");
        if (!Directory.Exists(installDir))
            throw new DirectoryNotFoundException($"install directory not found: {installDir}");

        var psi = new ProcessStartInfo
        {
            WorkingDirectory = installDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var bat = Path.Combine(installDir, "install-remote.bat");
            if (!File.Exists(bat))
                throw new FileNotFoundException("install-remote.bat not found.", bat);
            psi.FileName = bat;
            psi.ArgumentList.Add(job.Host);
            psi.ArgumentList.Add(job.User);
            psi.ArgumentList.Add(job.Password);
        }
        else
        {
            var sh = Path.Combine(installDir, "install-remote.sh");
            if (!File.Exists(sh))
                throw new FileNotFoundException("install-remote.sh not found.", sh);
            psi.FileName = "/bin/bash";
            psi.ArgumentList.Add(sh);
            psi.ArgumentList.Add(job.Host);
            psi.ArgumentList.Add(job.User);
            psi.ArgumentList.Add(job.Password);
        }

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start install-remote process.");

        _logger.LogInformation(
            "Cloner: started install-remote for {RunId} (pid {Pid})",
            job.RunId,
            proc.Id);

        var stdout = ReadStreamAsync(proc.StandardOutput, job.LogWriter, stoppingToken);
        var stderr = ReadStreamAsync(proc.StandardError, job.LogWriter, stoppingToken);
        await proc.WaitForExitAsync(stoppingToken).ConfigureAwait(false);
        await Task.WhenAll(stdout, stderr).ConfigureAwait(false); // drain after exit so buffers cannot deadlock

        await job.LogWriter.WriteAsync($"[exit] {proc.ExitCode}", stoppingToken).ConfigureAwait(false);
    }

    private static async Task ReadStreamAsync(StreamReader reader, ChannelWriter<string> log, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line == null)
                break;
            await log.WriteAsync(line, ct).ConfigureAwait(false);
        }
    }
}
