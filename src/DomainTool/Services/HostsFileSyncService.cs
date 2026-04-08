using System.Text;
using DomainTool.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DomainTool.Services;

public sealed class HostsFileSyncService
{
    private readonly IDomainNameSource _domains;
    private readonly IHostsFileComposer _composer;
    private readonly DomainToolOptions _options;
    private readonly ILogger<HostsFileSyncService> _logger;

    public HostsFileSyncService(
        IDomainNameSource domains,
        IHostsFileComposer composer,
        IOptions<DomainToolOptions> options,
        ILogger<HostsFileSyncService> logger)
    {
        _domains = domains;
        _composer = composer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var names = await _domains.GetEnabledDomainNamesAsync(cancellationToken).ConfigureAwait(false);
        var content = _composer.Compose(names);

        var path = _options.HostsPath;
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Wrote {Path} with {Count} domain(s) from database.", path, names.Count);
        return 0;
    }
}
