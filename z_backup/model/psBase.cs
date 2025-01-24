using System.Diagnostics;
using System.Security;
using model;

public abstract class PsBase
{
    private string Ip { get; }
    private string User { get; }
    private SecureString Password { get; }

    private ServerModel _serverModel { get; }

    private static string ScriptDir => ServerModelLoader.SysDirStatic;

    public string ScriptFile(string scriptName)
    {
        return Path.GetFullPath(Path.Combine(ScriptDir, scriptName + ".ps1"));
    }

    protected PsBase(ServerModel serverModel)
    {
        Ip = serverModel.Server;
        User = serverModel.Login;

        if (string.IsNullOrWhiteSpace(serverModel.Password) || serverModel.Password == "password")
        {
            var pass = Environment.GetEnvironmentVariable($"SuperPassword_{serverModel.Server}", EnvironmentVariableTarget.Machine);
            if (!(string.IsNullOrWhiteSpace(pass) || pass == "password"))
            {
                Password = ConvertToSecureString(pass);
            }
        }
        else
        {
            if (!(string.IsNullOrWhiteSpace(serverModel.Password) || serverModel.Password == "password"))
            {
                Password = ConvertToSecureString(serverModel.Password);
            }
        }

        _serverModel = serverModel;
    }

    private static SecureString ConvertToSecureString(string? password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");

        var secureString = new SecureString();
        foreach (var c in password)
            secureString.AppendChar(c);

        secureString.MakeReadOnly();
        return secureString;
    }

    protected List<string> ExecuteRemoteScript(string scriptFile, params (string Name, object Value)[] parameters)
    {
        scriptFile = ScriptFile(scriptFile);
        var result = ExecutePowerShellScript(scriptFile, parameters);
        return result;
    }
    
    public static List<string> ExecutePowerShellScript(string scriptFile, params (string Name, object Value)[] parameters)
    {
        var outputLines = new List<string>();

        // Build the arguments for the PowerShell script
        string scriptArguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\"";
        
        // Add parameters as arguments
        foreach (var param in parameters)
        {
            scriptArguments += $" -{param.Name} {param.Value}";
        }

        // Set up the process to run PowerShell
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe", // Use "pwsh" for PowerShell Core
            Arguments = scriptArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,  // Don't use the shell to execute
            CreateNoWindow = true     // Don't show the PowerShell window
        };

        try
        {
            // Start the process and capture output
            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    // Read the standard output
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        outputLines.AddRange(output.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
                    }

                    // Capture any errors
                    if (!string.IsNullOrEmpty(error))
                    {
                        outputLines.Add("ERROR: " + error);
                    }

                    process.WaitForExit(); // Wait for the script to finish executing
                }
            }
        }
        catch (Exception ex)
        {
            outputLines.Add($"Error: {ex.Message}");
        }

        return outputLines.Where(a=> a.Trim() != "").ToList();
    }

    public abstract List<string> Run(params (string Name, object Value)[] parameters);
}
