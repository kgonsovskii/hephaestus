using System.Reflection;

namespace model;

public static class VersionFetcher
{
    public static string Version()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                   .FirstOrDefault(a => a.Key == "BuildTimestamp")
                   ?.Value
               ?? "";
    }
}
