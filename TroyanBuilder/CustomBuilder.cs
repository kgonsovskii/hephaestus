using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using model;

namespace TroyanBuilder;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class CustomBuilder
{
    protected abstract string SourceDir {get;}
    
    protected abstract string OutputFile { get; }
    protected abstract string[] PrioritySources { get; }

    protected string[] PriorityLinks => PrioritySources.Select(x => $". ./{x}.ps1")
        .Union(PrioritySources.Select(x => $". ./holder/{x}.ps1")).ToArray();

    protected abstract string[] PriorityTasks { get; }
    protected abstract string[] UnpriorityTasks { get; }
    protected virtual string[] IgnoreTasks => new string[] {"holder"};
    
    private ServerService Svc;
    protected ServerModel Model = new();
    private List<string> SourceFiles;
    private Dictionary<string, string> SourceData = new Dictionary<string, string>();
    private List<string> DoFiles => SourceFiles
        .Except(PrioritySources)
        .Where(a=> !a.StartsWith("sub_")).Where(a=>!IgnoreTasks.Contains(a))
        .SortWithPriority(PriorityTasks, UnpriorityTasks).ToList();
    
    private readonly StringBuilder Builder = new();
    protected readonly List<string> Result = new();
    
    public virtual List<string> Build(string server)
    {
        Svc = new ServerService();
        var srv = Svc.GetServer(server, true, false, "localhost");
        Model = srv.ServerModel!;
        MakeConsts();
        InternalBuild(server);
        SourceFiles = GetSourceFiles();
        CompileSources();
        var directoryPath = Path.GetDirectoryName(OutputFile);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        Build();

        return Result;
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
            "Dir","holder","body","operation", "troyan", "dnSponsor", "ftp", "user", "alias","_operate","StatusLabel",
            "login", "password", "ico", "domainController",
            "interfaces", "bux", "landing", "php", "domainIp"
        };

        var serverFilePath = Model.UserServerFile;
        var serverJson = File.ReadAllText(serverFilePath);
        var server = JsonNode.Parse(serverJson)!;

        JsonNode FilterObjectByKeywords(JsonNode serverObject, List<string> filterKeywords)
        {
            var filteredDictionary = serverObject.AsObject()
                .Where(kvp => !filterKeywords.Any(a=> kvp.Key.ToLower().ToLower().Contains(a.ToLower())))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return JsonNode.Parse(JsonSerializer.Serialize(filteredDictionary))!;
        }

        var filteredObject = FilterObjectByKeywords(server, keywords);
        var serverJsonString = JsonSerializer.Serialize(filteredObject, new JsonSerializerOptions { WriteIndented = true });
        template = template.Replace("_SERVER", serverJsonString);

        var outputPath = Path.Combine(Model.TroyanScriptDir, "consts_body.ps1");
        File.WriteAllText(outputPath, template);
    }
    
    private bool IsDebug => this.GetType().Name.Contains("Debug");

    private void Build()
    {
        if (IsDebug)
        {
            BuildDebug();
        }
        else
        {
            BuildRelease();
        }
    }
    
    private void BuildDebug()
    {
        FlushSources();
        Builder.AppendLine("");
        foreach (var sourceFile in DoFiles)
        {
            var doX = $"do_{sourceFile}";
            Builder.AppendLine(doX);
        }
        
        File.WriteAllText(OutputFile,Builder.ToString());
    }

    private void BuildRelease()
    {
        var psString = new StringBuilder();
        foreach (var kvp in SourceData)
            psString.AppendLine($"    \"{kvp.Key}\" = \"{kvp.Value}\"");
        var doo = psString.ToString();
        
        var dataProd = Builder.ToString();
        var programRaw = ReadSource("program");
        (var head, var body) = ExtractHeadAndBody(programRaw);
        body = body.Replace("###doo", doo);
        dataProd = head + Environment.NewLine + dataProd + Environment.NewLine + body;
        File.WriteAllText(OutputFile,Builder.ToString());
    }

    protected abstract void InternalBuild(string server);
    
    private void CompileSources()
    {
        foreach (var sourceFile in SourceFiles)
        {
            var data = ReadSource(sourceFile);
            SourceData.Add(sourceFile, data);
        }
    }

    private void FlushSources()
    {
        foreach (var x in SourceData)
        {
            Builder.Append(x.Value);
            Builder.AppendLine();
        }
    }
    
    protected string PfxFile(string domain)
    {
        return Path.Combine(Model.CertDir, domain + ".pfx");
    }

    protected virtual List<string> GetSourceFiles()
    {
        var files =Directory.GetFiles(SourceDir)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray().Except(new[] { "program" }).ToList();
        var sortedArray = files.SortWithPriority(PrioritySources).ToList();
        return sortedArray;
    }

    private string ReadSource(string sourceFile)
    {
        var path = "";
        var dir = SourceDir;
        while (!File.Exists(path))
        {
            path = Path.Combine(dir, sourceFile + ".ps1");
            dir = Path.Combine(dir,"..");
        }
        var lines = File.ReadAllLines(path);
        var filteredLines = lines.Exclude(PriorityLinks);
        filteredLines = filteredLines.Exclude(SourceFiles.Select(a=> "do_" + a));
        
        var result = string.Join(Environment.NewLine, filteredLines);

        return result;
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

            if ( !headBegin || (headBegin && !headEnd))
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
}