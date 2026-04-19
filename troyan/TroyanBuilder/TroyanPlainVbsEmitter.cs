using Commons;

namespace TroyanBuilder;

/// <summary>Writes holder.vbs with standard base64 of plain holder.debug.ps1 (no randomer pass).</summary>
internal static class TroyanPlainVbsEmitter
{
    public static void Write(ServerLayoutPaths layout)
    {
        var templatePath = Path.Combine(layout.TroyanVbsDir, "holder.vbs");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("holder.vbs not found for plain VBS.", templatePath);

        var holderPs1 = layout.HolderPs1Debug;
        if (!File.Exists(holderPs1))
            throw new FileNotFoundException("holder.debug.ps1 must be built before plain VBS.", holderPs1);

        var b64 = Convert.ToBase64String(File.ReadAllBytes(holderPs1));
        var template = File.ReadAllText(templatePath);
        const string placeholder = "0102";
        if (!template.Contains(placeholder, StringComparison.Ordinal))
            throw new InvalidOperationException("holder.vbs must contain the 0102 placeholder.");

        var vbs = template.Replace(placeholder, b64, StringComparison.Ordinal);
        var dir = Path.GetDirectoryName(layout.TroyanPlainVbs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(layout.TroyanPlainVbs, vbs);
    }
}
