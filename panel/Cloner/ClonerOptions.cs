namespace Cloner;

public sealed class ClonerOptions
{
    public const string SectionName = "Cloner";

    /// <summary>Optional absolute path to Hephaestus repo root (folder that contains <c>install/</c>). Empty = walk up from app base.</summary>
    public string RepoRoot { get; set; } = "";
}
