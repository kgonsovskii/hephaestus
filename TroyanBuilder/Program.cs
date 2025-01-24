namespace TroyanBuilder;

class Program
{
    static void Main(string[] args)
    {
        var x = new TroyanBuilder();
        var result = x.Build(args.Length > 0 ? args[0] : "127.0.0.1");
        foreach (var line in result)
        {
            Console.WriteLine(line);    
        }
    }
}