using Domain;
using Microsoft.Extensions.Options;

namespace DataFtp;

public sealed class DataFtpUrlProvider : IDataFtpUrlProvider
{
    private readonly IWebContentPathProvider _webPaths;
    private readonly IOptions<DataFtpOptions> _options;

    public DataFtpUrlProvider(IWebContentPathProvider webPaths, IOptions<DataFtpOptions> options)
    {
        _webPaths = webPaths;
        _options = options;
    }

    public string WebRootFullPath => _webPaths.WebRootFullPath;

    public int Port => _options.Value.Port;

    public string BuildUrl(string hostName)
    {
        var host = hostName.Trim();
        if (host.Length == 0)
            host = "localhost";

        var port = Port;
        var user = Uri.EscapeDataString(DataFtpConstants.UserName);
        var pass = Uri.EscapeDataString(DataFtpConstants.Password);
        var authority = port == 21
            ? $"{user}:{pass}@{host}"
            : $"{user}:{pass}@{host}:{port}";
        return $"ftp://{authority}/";
    }
}
