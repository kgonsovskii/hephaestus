namespace cp;

public static class CpSettings
{
        public const string SitePathPrefix = "/cp";

    public static string SuperHost => Environment.GetEnvironmentVariable("SuperHost", EnvironmentVariableTarget.Machine)!;

    public static string RemoteUrl => $"http://{SuperHost}";

    public static bool IsSuperHost => !string.IsNullOrEmpty(SuperHost);
}
