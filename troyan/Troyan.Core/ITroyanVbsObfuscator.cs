namespace Troyan.Core;

/// <summary>Obfuscates VBS launcher text for the final <c>troyan.vbs</c> (mgeeky-inspired, WSH-safe).</summary>
public interface ITroyanVbsObfuscator
{
    string Obfuscate(string vbsText);
}
