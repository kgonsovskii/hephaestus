namespace Troyan.Core;

public interface IPowerShellObfuscator
{
    /// <summary>Placeholder segment for holder sources (replaces <c>###random</c>).</summary>
    string RandomCode();
}
