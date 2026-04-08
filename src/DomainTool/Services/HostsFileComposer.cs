using System.Text;
using Microsoft.Extensions.Logging;

namespace DomainTool.Services;

public sealed class HostsFileComposer : IHostsFileComposer
{
    private readonly ILogger<HostsFileComposer> _logger;

    public HostsFileComposer(ILogger<HostsFileComposer> logger) => _logger = logger;

    public string Compose(IReadOnlyList<string> domainNames)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("127.0.0.1       localhost");
        sb.AppendLine("::1             localhost");
        sb.AppendLine();

        foreach (var raw in domainNames)
        {
            var safe = SanitizeHostForHostsFile(raw);
            if (safe is null)
            {
                _logger.LogWarning("Skipped invalid domain value for hosts file: {Domain}", raw);
                continue;
            }

            sb.Append("127.0.0.1\t");
            sb.AppendLine(safe);
        }

        return sb.ToString();
    }

    private static string? SanitizeHostForHostsFile(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return null;
        var t = domain.Trim();
        if (t.AsSpan().IndexOfAny("\r\n\t #".ToCharArray()) >= 0)
            return null;
        if (t.StartsWith('-') || t.EndsWith('-') || t.StartsWith('.') || t.EndsWith('.'))
            return null;
        if (!t.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            return null;
        return t.ToLowerInvariant();
    }
}
