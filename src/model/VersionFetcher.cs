using System.Reflection;

namespace model;

public static class VersionFetcher
{
    public static string Version()
    {
        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyMetadataAttribute>()?
            .Value;
        return version;
    }
}