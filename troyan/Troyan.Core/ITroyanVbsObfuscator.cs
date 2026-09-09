namespace Troyan.Core;

/// <summary>Obfuscates VBS launcher text for the final <c>troyan.vbs</c> (stub may copy raw).</summary>
public interface ITroyanVbsObfuscator
{
    string Obfuscate(string vbsText);
}
