namespace TroyanBuilder;

class Program
{
    static void Main(string[] args)
    {
        var arr = new CustomBuilder[]{new BodyBuilderDebug(), new BodyBuilderRelease(), new HolderBuilderDebug(), new HolderBuilderRelease()};
        //var arr = new CustomBuilder[]{ new HolderBuilderDebug()};
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