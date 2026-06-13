namespace Git;

public static class HephaestusDataGitConstants
{
    public const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_data.git";

    public const string AccessToken =
        "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";

    public static string CloneUrl =>
        $"https://x-access-token:{AccessToken}@github.com/kgonsovskii/hephaestus_data.git";

    /// <summary>First chars of PAT for logs (verify deployed build matches source).</summary>
    public static string TokenFingerprint =>
        AccessToken.Length >= 20 ? AccessToken[..20] + "..." : AccessToken;
}
