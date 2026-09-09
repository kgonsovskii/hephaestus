using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Troyan.Core;

/// <summary>
/// WSH VBScript obfuscator adapted from
/// <a href="https://github.com/mgeeky/VisualBasicObfuscator">mgeeky/VisualBasicObfuscator</a>
/// (Knuth bit-shuffle strings, comment strip, identifier rename).
/// Upstream emits VBA and cannot run under <c>cscript</c>; this port keeps valid VBS.
/// Bit-shuffled bytes are hex-encoded (not base64) because WSH cannot safely turn
/// MSXML binary variants into byte values without charset corruption.
/// </summary>
public sealed class TroyanVbsObfuscator : ITroyanVbsObfuscator
{
    private const char StringPaddingChar = '~';
    private const uint Mask1 = 0x00550055;
    private const uint Mask2 = 0x0000cccc;
    private const int D1 = 7;
    private const int D2 = 14;
    private const int MinVarLength = 5;
    private const int ShortStringMaxLen = 5;

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "And", "Or", "Xor", "Not", "Mod", "Eqv", "Imp", "Is", "Like",
        "Dim", "ReDim", "Preserve", "Set", "Let", "Get", "Const", "New", "Me",
        "Sub", "Function", "End", "Exit", "Call", "If", "Then", "Else", "ElseIf",
        "Select", "Case", "Loop", "While", "Wend", "Do", "Until", "For", "Each",
        "Next", "To", "Step", "With", "Option", "Explicit", "Private", "Public",
        "Class", "Property", "ByVal", "ByRef", "Optional", "ParamArray", "As",
        "Boolean", "Byte", "Integer", "Long", "Single", "Double", "Currency",
        "Date", "String", "Object", "Variant", "Error", "On", "Resume", "Goto",
        "GoSub", "Stop", "Rem", "Erase", "True", "False", "Empty", "Null", "Nothing",
        "WScript", "CreateObject", "GetObject", "GetRef", "Eval", "Execute", "ExecuteGlobal",
        "MsgBox", "InputBox", "Chr", "ChrW", "ChrB", "Asc", "AscW", "AscB",
        "Mid", "MidB", "Left", "LeftB", "Right", "RightB", "Len", "LenB", "Trim",
        "LTrim", "RTrim", "LCase", "UCase", "InStr", "InStrB", "InStrRev", "Replace",
        "Split", "Join", "Array", "Int", "Fix", "CLng", "CInt", "CByte", "CStr",
        "CBool", "CDate", "CDbl", "CSng", "CCur", "IsEmpty", "IsNull", "IsObject",
        "IsNumeric", "IsArray", "IsDate", "TypeName", "VarType", "Now", "Timer",
        "RGB", "Round", "Rnd", "Randomize", "Hex", "Oct", "Space",
        "vbCr", "vbLf", "vbCrLf", "vbTab", "vbNullString", "vbNullChar",
        "vbBinaryCompare", "vbTextCompare", "vbFromUnicode", "vbUnicode",
    };

    public string Obfuscate(string vbsText)
    {
        ArgumentException.ThrowIfNullOrEmpty(vbsText);

        var rng = Random.Shared;
        var deobfName = "D" + RandomIdent(rng, 10);
        // Rename before string hiding so replacements never touch payloads.
        var output = RemoveComments(vbsText);
        output = RemoveEmptyLines(output);
        output = RenameIdentifiers(output, rng);
        output = ObfuscateStrings(output, deobfName, rng);
        output = RemoveEmptyLines(CollapseHorizontalWhitespace(output));
        output = output.TrimEnd() + Environment.NewLine + BuildDeobfuscatorVbs(deobfName) + Environment.NewLine;
        return output;
    }

    private static string ObfuscateStrings(string input, string deobfName, Random rng)
    {
        return Regex.Replace(
            input,
            "\"(?:[^\"]|\"\")*\"",
            m =>
            {
                var orig = m.Value;
                var content = UnescapeVbsString(orig);
                if (content.Length == 0)
                    return orig;

                // Embedded PS body is already base64 — re-encoding only inflates size.
                if (LooksLikeOpaqueBase64(content))
                    return orig;

                if (content.Length <= ShortStringMaxLen || content.Contains(StringPaddingChar))
                    return EncodeShortString(content, rng);

                return deobfName + "(\"" + BitShuffleToHex(content) + "\")";
            },
            RegexOptions.CultureInvariant);
    }

    private static bool LooksLikeOpaqueBase64(string s)
    {
        if (s.Length < 256)
            return false;
        foreach (var ch in s)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '+' or '/' or '=' or '\r' or '\n')
                continue;
            return false;
        }

        return true;
    }

    private static string EncodeShortString(string content, Random rng)
    {
        var parts = new List<string>(content.Length);
        foreach (var ch in content)
        {
            var n = (int)ch;
            parts.Add(rng.Next(3) switch
            {
                0 => "Chr(" + n.ToString(CultureInfo.InvariantCulture) + ")",
                1 => "Chr(&H" + n.ToString("X", CultureInfo.InvariantCulture) + ")",
                _ => "\"" + EscapeVbsString(ch.ToString()) + "\"",
            });
        }

        return string.Join("&", parts);
    }

    /// <summary>mgeeky bit-shuffle, hex-encoded for reliable WSH decoding.</summary>
    public static string BitShuffleToHex(string stringContent)
    {
        var chars = stringContent.ToCharArray().ToList();
        if (chars.Count % 4 != 0)
        {
            var pad = 4 - (chars.Count % 4);
            for (var i = 0; i < pad; i++)
                chars.Add(StringPaddingChar);
        }

        var obfuscated = new byte[chars.Count];
        var o = 0;
        for (var i = 0; i < chars.Count; i += 4)
        {
            var dword = ComposeDword(chars[i + 3], chars[i + 2], chars[i + 1], chars[i]);
            var obfuscatedDword = UintObfuscate(dword);
            obfuscated[o++] = (byte)(obfuscatedDword & 0xFF);
            obfuscated[o++] = (byte)((obfuscatedDword >> 8) & 0xFF);
            obfuscated[o++] = (byte)((obfuscatedDword >> 16) & 0xFF);
            obfuscated[o++] = (byte)((obfuscatedDword >> 24) & 0xFF);
        }

        return Convert.ToHexString(obfuscated);
    }

    public static string BitShuffleFromHex(string hex)
    {
        var raw = Convert.FromHexString(hex);
        var sb = new StringBuilder(raw.Length);
        for (var fr = 0; fr + 3 < raw.Length; fr += 4)
        {
            var dword = ((uint)raw[fr + 3] << 24)
                        | ((uint)raw[fr + 2] << 16)
                        | ((uint)raw[fr + 1] << 8)
                        | raw[fr];
            var restored = UintRestore(dword);
            var a = (char)((restored >> 24) & 0xFF);
            var b = (char)((restored >> 16) & 0xFF);
            var c = (char)((restored >> 8) & 0xFF);
            var d = (char)(restored & 0xFF);
            sb.Append(d).Append(c).Append(b).Append(a);
        }

        var outStr = sb.ToString();
        while (outStr.EndsWith(StringPaddingChar))
            outStr = outStr[..^1];
        return outStr;
    }

    private static uint ComposeDword(char a, char b, char c, char d) =>
        ((uint)(byte)a << 24) | ((uint)(byte)b << 16) | ((uint)(byte)c << 8) | (byte)d;

    private static uint UintObfuscate(uint num)
    {
        var t = (num ^ (num >> D1)) & Mask1;
        var u = num ^ t ^ (t << D1);
        t = (u ^ (u >> D2)) & Mask2;
        return u ^ t ^ (t << D2);
    }

    private static uint UintRestore(uint num)
    {
        var t = (num ^ (num >> D2)) & Mask2;
        var u = num ^ t ^ (t << D2);
        t = (u ^ (u >> D1)) & Mask1;
        return u ^ t ^ (t << D1);
    }

    private static string BuildDeobfuscatorVbs(string name)
    {
        // Hex + Knuth restore (mgeeky), without VBA types / StrConv / binary charset hacks.
        return $$"""
Function shr{{name}}(Value,Shift)
shr{{name}}=Value
If Shift>0 Then
If Value>0 Then
shr{{name}}=Int(shr{{name}}/(2^Shift))
Else
If Shift>31 Then
shr{{name}}=0
Else
shr{{name}}=shr{{name}} And &H7FFFFFFF
shr{{name}}=Int(shr{{name}}/(2^Shift))
shr{{name}}=shr{{name}} Or 2^(31-Shift)
End If
End If
End If
End Function
Function shl{{name}}(Value,Shift)
shl{{name}}=Value
If Shift>0 Then
Dim i{{name}},m{{name}}
For i{{name}}=1 To Shift
m{{name}}=shl{{name}} And &H40000000
shl{{name}}=(shl{{name}} And &H3FFFFFFF)*2
If m{{name}}<>0 Then
shl{{name}}=shl{{name}} Or &H80000000
End If
Next
End If
End Function
Function dd{{name}}(num)
Dim t{{name}},u{{name}}
t{{name}}=(num Xor shr{{name}}(num,14)) And 52428
u{{name}}=num Xor t{{name}} Xor shl{{name}}(t{{name}},14)
t{{name}}=(u{{name}} Xor shr{{name}}(u{{name}},7)) And 5570645
dd{{name}}=u{{name}} Xor t{{name}} Xor shl{{name}}(t{{name}},7)
End Function
Function hb{{name}}(hx)
hb{{name}}=CLng("&H" & hx)
End Function
Function {{name}}(inputString)
Dim i{{name}},dword{{name}},raw{{name}},deobf{{name}},a{{name}},b{{name}},c{{name}},d{{name}},b0{{name}},b1{{name}},b2{{name}},b3{{name}}
deobf{{name}}=""
inputString=UCase(Replace(Replace(inputString,vbCr,""),vbLf,""))
For i{{name}}=1 To Len(inputString) Step 8
If i{{name}}+7>Len(inputString) Then Exit For
b0{{name}}=hb{{name}}(Mid(inputString,i{{name}},2))
b1{{name}}=hb{{name}}(Mid(inputString,i{{name}}+2,2))
b2{{name}}=hb{{name}}(Mid(inputString,i{{name}}+4,2))
b3{{name}}=hb{{name}}(Mid(inputString,i{{name}}+6,2))
dword{{name}}=b0{{name}} Or shl{{name}}(b1{{name}},8) Or shl{{name}}(b2{{name}},16) Or shl{{name}}(b3{{name}},24)
raw{{name}}=dd{{name}}(dword{{name}})
a{{name}}=Chr(shr{{name}}(raw{{name}} And &HFF000000,24) And 255)
b{{name}}=Chr(shr{{name}}(raw{{name}} And 16711680,16) And 255)
c{{name}}=Chr(shr{{name}}(raw{{name}} And 65280,8) And 255)
d{{name}}=Chr(raw{{name}} And 255)
deobf{{name}}=deobf{{name}} & d{{name}} & c{{name}} & b{{name}} & a{{name}}
Next
Do While Len(deobf{{name}})>0 And Right(deobf{{name}},1)="{{StringPaddingChar}}"
deobf{{name}}=Left(deobf{{name}},Len(deobf{{name}})-1)
Loop
{{name}}=deobf{{name}}
End Function
""";
    }

    private static string RenameIdentifiers(string input, Random rng)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in Regex.Matches(input, @"\b(?:Function|Sub)\s+(\w+)\b", RegexOptions.IgnoreCase))
            ConsiderName(names, m.Groups[1].Value);

        foreach (Match m in Regex.Matches(input, @"\bDim\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
        {
            foreach (Match id in Regex.Matches(m.Groups[1].Value, @"\b([A-Za-z][A-Za-z0-9_]*)\b"))
            {
                var v = id.Groups[1].Value;
                if (v.Equals("As", StringComparison.OrdinalIgnoreCase))
                    continue;
                ConsiderName(names, v);
            }
        }

        foreach (Match m in Regex.Matches(input, @"\bSet\s+(\w+)\s*=", RegexOptions.IgnoreCase))
            ConsiderName(names, m.Groups[1].Value);

        var map = names
            .OrderByDescending(n => n.Length)
            .ToDictionary(n => n, _ => "V" + RandomIdent(rng, rng.Next(8, 14)), StringComparer.OrdinalIgnoreCase);

        if (map.Count == 0)
            return input;

        return Regex.Replace(
            input,
            "\"(?:[^\"]|\"\")*\"|\\b([A-Za-z][A-Za-z0-9_]*)\\b",
            m =>
            {
                if (m.Value.StartsWith('"'))
                    return m.Value;
                return map.TryGetValue(m.Groups[1].Value, out var to) ? to : m.Value;
            },
            RegexOptions.CultureInvariant);
    }

    private static void ConsiderName(HashSet<string> names, string name)
    {
        if (name.Length < MinVarLength)
            return;
        if (Reserved.Contains(name))
            return;
        names.Add(name);
    }

    private static string RemoveComments(string input)
    {
        var lines = input.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var inString = false;
            for (var j = 0; j < line.Length; j++)
            {
                var ch = line[j];
                if (ch == '"')
                {
                    if (inString && j + 1 < line.Length && line[j + 1] == '"')
                    {
                        j++;
                        continue;
                    }

                    inString = !inString;
                    continue;
                }

                if (!inString && ch == '\'')
                {
                    lines[i] = line[..j].TrimEnd();
                    break;
                }
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string RemoveEmptyLines(string txt) =>
        string.Join(Environment.NewLine, txt.Replace("\r\n", "\n").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)));

    private static string CollapseHorizontalWhitespace(string txt) =>
        Regex.Replace(txt, @"[ \t]{2,}", " ");

    private static string UnescapeVbsString(string quoted)
    {
        var inner = quoted[1..^1];
        return inner.Replace("\"\"", "\"", StringComparison.Ordinal);
    }

    private static string EscapeVbsString(string s) => s.Replace("\"", "\"\"", StringComparison.Ordinal);

    private static string RandomIdent(Random rng, int len)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sb = new StringBuilder(len);
        for (var i = 0; i < len; i++)
            sb.Append(alphabet[rng.Next(alphabet.Length)]);
        return sb.ToString();
    }
}
