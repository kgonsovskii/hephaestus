namespace cp;

/// <summary>Runtime flags and paths for the control-panel site mounted at <see cref="SitePathPrefix"/>.</summary>
public static class CpSettings
{
    /// <summary>Hardcoded URL segment where DomainHost mounts the cp pipeline (e.g. <c>https://host/cp/...</c>).</summary>
    public const string SitePathPrefix = "/cp";

    public static string SuperHost => Environment.GetEnvironmentVariable("SuperHost", EnvironmentVariableTarget.Machine)!;

    public static string RemoteUrl => $"http://{SuperHost}";

    public static bool IsSuperHost => !string.IsNullOrEmpty(SuperHost);
}
