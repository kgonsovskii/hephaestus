using Domain;
using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DataFtp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataFtp(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DataFtpOptions>(configuration.GetSection(DataFtpOptions.SectionName));
        services.AddSingleton<IMembershipProvider, DataFtpMembershipProvider>();
        services.AddFtpServer(builder => builder
            .UseDotNetFileSystem()
            .UseSingleRoot());
        services.AddOptions<FtpServerOptions>()
            .Configure<IOptions<DataFtpOptions>>((ftp, dataFtp) =>
            {
                ftp.ServerAddress = string.Empty;
                ftp.Port = dataFtp.Value.Port;
            });
        services.AddOptions<DotNetFileSystemOptions>()
            .Configure<IWebContentPathProvider>((opts, webPaths) =>
            {
                opts.RootPath = webPaths.WebRootFullPath;
                opts.AllowNonEmptyDirectoryDelete = true;
            });
        services.AddSingleton<IDataFtpUrlProvider, DataFtpUrlProvider>();
        services.AddHostedService<DataFtpHostedService>();
        return services;
    }
}
