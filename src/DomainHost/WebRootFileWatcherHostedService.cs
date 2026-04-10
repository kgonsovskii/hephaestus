using Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DomainHost;

/// <summary>Watches <see cref="IWebContentPathProvider.WebRootFullPath"/> and bumps <see cref="WebStaticRevision"/> on changes (debounced).</summary>
public sealed class WebRootFileWatcherHostedService : BackgroundService
{
    private readonly IWebContentPathProvider _paths;
    private readonly WebStaticRevision _revision;
    private readonly ILogger<WebRootFileWatcherHostedService> _logger;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private CancellationToken _appStopping;

    public WebRootFileWatcherHostedService(
        IWebContentPathProvider paths,
        WebStaticRevision revision,
        ILogger<WebRootFileWatcherHostedService> logger)
    {
        _paths = paths;
        _revision = revision;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _appStopping = stoppingToken;
        var root = _paths.WebRootFullPath;
        using var watcher = new FileSystemWatcher(root)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size |
                           NotifyFilters.CreationTime
        };

        void OnEvent(object? _, FileSystemEventArgs __) => ScheduleBump();

        watcher.Changed += OnEvent;
        watcher.Created += OnEvent;
        watcher.Deleted += OnEvent;
        watcher.Renamed += (_, _) => ScheduleBump();
        watcher.Error += (_, e) => _logger.LogError(e.GetException(), "Web root FileSystemWatcher error.");

        watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Watching web root for static cache invalidation: {Path}", root);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        watcher.EnableRaisingEvents = false;
    }

    private void ScheduleBump()
    {
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(_appStopping);
            var token = _debounceCts.Token;
            _ = DebounceBumpAsync(token);
        }
    }

    private async Task DebounceBumpAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var rev = _revision.Bump();
        _logger.LogDebug("Web root content changed; static cache revision is now {Revision}.", rev);
    }
}
