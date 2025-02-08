using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Text;

namespace TroyanBuilder
{
    public static class PowerShellObfuscator
    {
        private static readonly Random Random = new();
        private static readonly Dictionary<string,Dictionary<string, string>> Renamed = new();
        private static readonly HashSet<string> GeneratedNames = new();

        public static string Obfuscate(string psScript)
        {
            var parsed = Parse(psScript);
            var result = FindAndRenameVariables(parsed, "general");
            result = FindAndRenameFunctions(parsed, "general");
           // result = ObfuscateFunctions(result, scope);
            return result;
        }

        public class Parsed
        {
            public string PsScript {get; set;}
            public ScriptBlockAst Body { get; set; }
            public Token[] Tokens { get; set; }
            
            public ParseError[] Errors { get; set; }
        }

        private static Parsed Parse(string psScript)
        {
            var body = Parser.ParseInput(psScript, out Token[] tokens, out ParseError[] errors);
            if (errors.Length > 0)
                throw new Exception("PowerShell script contains syntax errors!");
            return new Parsed() {Body = body, Tokens = tokens, Errors = errors, PsScript = psScript };
        }

        private static string Obfuscate(Parsed parsed, string scope)
        {
            var sb = new StringBuilder(parsed.PsScript.Length);
            int lastPos = 0;
            foreach (var token in parsed.Tokens)
            {
                if (token.Extent.StartOffset > lastPos && lastPos >= 0 && token.Extent.StartOffset <= parsed.PsScript.Length)
                {
                    var q = parsed.PsScript.AsSpan(lastPos, token.Extent.StartOffset - lastPos);
                    sb.Append(q);
                }

                if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.Generic || token.Kind == TokenKind.Variable)
                {
                    string tokenText = token.Text;
                    if (token.Kind == TokenKind.Variable)
                    {
                        string variableName = tokenText.TrimStart('$');
                        if (TryGetRenamed(scope, variableName, out var renamedVar))
                        {
                            sb.Append('$' + renamedVar);
                        }
                        else
                        {
                            sb.Append(tokenText);
                        }
                    }
                    else if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.Generic)
                    {
                        string tokenName = tokenText;
                        if (TryGetRenamed(scope, tokenName, out var renamedVar))
                        {
                            sb.Append(renamedVar);
                        }
                        else
                        {
                            sb.Append(tokenText);
                        }
                    }
                    else
                    {
                        sb.Append(tokenText);
                    }
                }
                else
                {
                    sb.Append(token.Text);
                }

                lastPos = token.Extent.EndOffset;
            }

            if (lastPos >= 0 && lastPos < parsed.PsScript.Length)
            {
                sb.Append(parsed.PsScript.AsSpan(lastPos));
            }

            return sb.ToString();
        }

        public class VarInfo
        {
            public string Name { get; set; }
            public bool IsParameter { get; set; }
            public bool isFunction { get; set; }
            public string Scope { get; set; }
        }

        private static List<string> Exclusions = new List<string>()
        {
            "true", "false", "PSCommandPath", "MyInvocation", "MyCommand", "Path", "Definition"
        };

        private static VarInfo GetInfo(VariableExpressionAst variable)
        {
            var result = new VarInfo();
            result.Scope = "general";
            string variableName = variable.VariablePath.UserPath;
            result.Name = variableName.TrimStart('$');
            if (Exclusions.Select(a=> a.ToLower()).Contains(variableName.ToLower()))
                result.IsParameter = true;
            
            var parent = variable.Parent;
            while (parent != null)
            {
                if (parent is ParamBlockAst)
                {
                    result.IsParameter = true;
                }
                if (parent is FunctionDefinitionAst)
                {
                    result.isFunction = true;
                    result.Scope = ((FunctionDefinitionAst)parent).Name;
                }
                parent = parent.Parent;
            }
            return result;
        }


        private static string FindAndRenameVariables(Parsed parsed, string scope)
        {
            var vars = parsed.Body.FindAll(x => x is VariableExpressionAst, false).Cast<VariableExpressionAst>();
            foreach (var variableAst in vars )
            {
                var info = GetInfo(variableAst);
      
    
                var newname = info.IsParameter ? info.Name : GenerateRandomName();
                AddRenamed(info.Scope, info.Name, newname);
            }
            var result = Obfuscate(parsed, scope);
            return result;
        }
        
                        
        private static string FindAndRenameFunctions(Parsed parsed, string scope)
        {
            var funcs = parsed.Body.FindAll(x => x is FunctionDefinitionAst, false).Cast<FunctionDefinitionAst>();
            foreach (var functionAst in funcs)
            {
                AddRenamed(scope, functionAst.Name, GenerateRandomName());
            }
            var result = Obfuscate(parsed, scope);
            return result;
        }


        private static string ObfuscateFunctions(string psScript, string scope)
        {
            var parsed = Parse(psScript);
            foreach (var functionAst in parsed.Body.FindAll(x => x is FunctionDefinitionAst, false).Cast<FunctionDefinitionAst>())
            {
                ObfuscateFunction(functionAst);
            }

            return Obfuscate(parsed, scope);
        }

        private static void ObfuscateFunction(FunctionDefinitionAst functionAst)
        {
            var psScript = functionAst.Body.ToString();
            var parsed = Parse(psScript);
            var result = FindAndRenameVariables(parsed, functionAst.Name);
            result = Obfuscate(parsed, functionAst.Name);
        }

        private static void AddRenamed(string scope, string name, string newName)
        {
            scope="general";
            Renamed.TryAdd(scope, new Dictionary<string, string>());
            Renamed[scope].TryAdd(name, newName);
        }

        private static bool TryGetRenamed(string scope, string name, out string newName)
        {
            scope="general";
            newName = name;
            var got = Renamed.TryGetValue(scope, out var dict);
            if (!got)
                return false;
            got = dict!.TryGetValue(name, out var x);
            if (!got)
                return false;
            newName = x;
            return true;
        }
        
        private static string GenerateRandomName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomName;

            do
            {
                var len = Random.Next(17, 23);
                randomName = new string(Enumerable.Repeat(chars, len).Select(s => s[Random.Next(s.Length)]).ToArray());
            } 
            while (GeneratedNames.Contains(randomName));

            GeneratedNames.Add(randomName);

            return randomName;
        }
    }
}
