namespace Troyan.Core;

/// <summary>Stub VBS obfuscator: returns the input unchanged until a real transform is implemented.</summary>
public sealed class TroyanVbsObfuscator : ITroyanVbsObfuscator
{
    public string Obfuscate(string vbsText)
    {
        ArgumentException.ThrowIfNullOrEmpty(vbsText);
        return vbsText;
    }
}
