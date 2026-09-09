namespace Troyan.Core;

/// <summary>Appsettings section <c>Obfuscation</c> — toggles for final <c>troyan.vbs</c> / <c>troyan.cmd</c>.</summary>
public sealed class TroyanObfuscationOptions
{
    public const string SectionName = "Obfuscation";

    /// <summary>When false, final <c>troyan.vbs</c> is a copy of the plain launcher (default: false).</summary>
    public bool Vbs { get; set; }

    /// <summary>When true, final <c>troyan.cmd</c> is obfuscated (default: true).</summary>
    public bool Cmd { get; set; } = true;
}
