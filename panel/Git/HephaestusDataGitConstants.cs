namespace Git;

internal static class HephaestusDataGitConstants
{
    internal const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_data.git";

    internal const string AccessToken =
        "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";

    internal static string CloneUrl =>
        $"https://x-access-token:{AccessToken}@github.com/kgonsovskii/hephaestus_data.git";
}
