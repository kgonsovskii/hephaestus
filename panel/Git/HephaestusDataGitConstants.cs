namespace Git;

public static class HephaestusDataGitConstants
{
    public const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_data.git";

    public const string AccessToken =
        "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";

    public static string CloneUrl =>
        $"https://x-access-token:{AccessToken}@github.com/kgonsovskii/hephaestus_data.git";
}
