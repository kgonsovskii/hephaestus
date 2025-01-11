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
            Password = ConvertToSecureString(pass);
        }
        else
        {
            Password = ConvertToSecureString(serverModel.Password);
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
        return new List<string>();
       /* scriptFile = ScriptFile(scriptFile);
        var script = File.ReadAllText(scriptFile);
        var results = new List<string>();

        var credential = new PSCredential(User, Password);
        
        var connectionUri = new Uri($"http://{Ip}:5985/wsman");
        var connectionInfo = new WSManConnectionInfo(connectionUri,
            "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential)
        {
            AuthenticationMechanism = AuthenticationMechanism.Basic,
            NoEncryption = true
        };
        
        using (var runspace = RunspaceFactory.CreateRunspace(connectionInfo))
        {
            runspace.Open();

            using (var pipeline = runspace.CreatePipeline())
            {
                pipeline.Commands.AddScript(script);
                
                foreach (var parameter in parameters)
                {
                    pipeline.Commands[0].Parameters.Add(parameter.Name, parameter.Value);
                }
                
                var psResults = pipeline.Invoke();
                
                foreach (var psObject in psResults)
                {
                    results.Add(psObject.ToString());
                }
            }
            
            runspace.Close();
        }

        return results;*/
    }

    public abstract List<string> Run(params (string Name, object Value)[] parameters);
}
