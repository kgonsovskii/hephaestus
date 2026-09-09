namespace Troyan.Core;

/// <summary>Build-time light obfuscation for emitted <c>troyan.cmd</c> (random markers + junk noise; no PowerShell).</summary>
public interface ITroyanCmdObfuscator
{
    string Obfuscate(string cmdText);
}
