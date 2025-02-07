using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

/*
 *
 * https://spy-soft.net/powershell-script-obfuscation/?ysclid=m6v68dunlr653293438
 * https://github.com/danielbohannon/Invoke-Obfuscation
 */
public static class PowerShellObfuscator
{
    private static readonly Random Random = new();

    public static string Obfuscate(string psScript)
    {
        // Parse the PowerShell script
        Token[] tokens;
        ParseError[] errors;
        var ast = Parser.ParseInput(psScript, out tokens, out errors);

        if (errors.Length > 0)
            throw new Exception("PowerShell script contains syntax errors!");

        var sb = new StringBuilder();

        foreach (var token in tokens)
        {
            // Obfuscate each token based on its type
            if (token.Kind == TokenKind.Identifier)
            {
                // Rename variables (e.g., $myVar -> $xAZ1_)
                sb.Append(ObfuscateVariable(token.Text));
            }
            else if (token.Kind == TokenKind.StringLiteral)
            {
                // Encode strings with base64
                sb.Append(ObfuscateString(token.Text));
            }
            else
            {
                sb.Append(token.Text);
            }

            // Randomly add noise (comments, new lines)
            if (Random.Next(0, 4) == 0)
            {
                sb.Append(GenerateNoise());
            }

            sb.Append(" ");
        }

        return sb.ToString();
    }

    private static string ObfuscateVariable(string varName)
    {
        if (!varName.StartsWith("$")) return varName;

        string randomVar = "$" + GenerateRandomString(5);
        return randomVar;
    }

    private static string ObfuscateString(string str)
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        return $"[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(\"{base64}\"))";
    }

    private static string GenerateNoise()
    {
        string[] noiseOptions = {
            "`n`r", // New lines
            "`t", // Tabs
            "# " + GenerateRandomString(5) + "`n", // Random comments
            "$" + GenerateRandomString(4) + " = " + Random.Next(100, 999) + ";", // Junk variables
        };

        return noiseOptions[Random.Next(noiseOptions.Length)];
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}
