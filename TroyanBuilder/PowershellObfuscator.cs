using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;

namespace TroyanBuilder;

public static class PowerShellObfuscator
{
    private static readonly Random Random = new();
    private static readonly Dictionary<string, string> RenamedFunctions = new();

    public static string Obfuscate(string psScript)
    {
        var ast = Parser.ParseInput(psScript, out Token[] tokens, out ParseError[] errors);

        if (errors.Length > 0)
            throw new Exception("PowerShell script contains syntax errors!");

        FindAndRenameFunctions(ast);

        var sb = new StringBuilder(psScript.Length);

        int lastPos = 0;
        foreach (var token in tokens)
        {
            if (token.Extent.StartOffset > lastPos && lastPos >= 0 && token.Extent.StartOffset <= psScript.Length)
            {
                sb.Append(psScript.AsSpan(lastPos, token.Extent.StartOffset - lastPos));
            }

            if (token.Kind == TokenKind.Identifier && RenamedFunctions.TryGetValue(token.Text, out var newName))
            {
                sb.Append(newName);
            }
            else
            {
                sb.Append(token.Text);
            }

            lastPos = token.Extent.EndOffset;
        }

        if (lastPos >= 0 && lastPos < psScript.Length)
        {
            sb.Append(psScript.AsSpan(lastPos));
        }

        return sb.ToString();
    }

    private static void FindAndRenameFunctions(Ast ast)
    {
        foreach (var functionAst in ast.FindAll(x => x is FunctionDefinitionAst, false).Cast<FunctionDefinitionAst>())
        {
            if (!RenamedFunctions.ContainsKey(functionAst.Name))
            {
                RenamedFunctions[functionAst.Name] = GenerateRandomName();
            }
        }
    }

    private static string GenerateRandomName()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}
