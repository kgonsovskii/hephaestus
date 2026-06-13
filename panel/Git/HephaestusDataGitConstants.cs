namespace Git;

internal static class HephaestusDataGitConstants
{
    internal const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_data.git";

    /// <summary>Fallback when <c>Git:HephaestusDataAccessToken</c> is not set in appsettings. Must allow push (Contents read+write).</summary>
    internal const string DefaultAccessToken =
        "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";
}
