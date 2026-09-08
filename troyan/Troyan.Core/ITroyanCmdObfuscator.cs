namespace Troyan.Core;

/// <summary>Build-time light obfuscation for emitted <c>troyan.cmd</c> (markers, <c>-EncodedCommand</c>, junk noise).</summary>
public interface ITroyanCmdObfuscator
{
    string Obfuscate(string cmdText);
}
