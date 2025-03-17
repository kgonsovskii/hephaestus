using System;
using System.IO.Compression;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace SharpRDP
{
    class Program
    {
        static Dictionary<string, string> ParseArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    var parts = arg.Substring(2).Split('=');
                    if (parts.Length >= 2)
                    {
                        dict[parts[0]] = string.Join("=", parts, 1, parts.Length - 1); // Preserve '=' in value
                    }
                }
            }
            return dict;
        }
        
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, argtwo) => {
                Assembly thisAssembly = Assembly.GetEntryAssembly();
                String resourceName = string.Format("SharpRDP.{0}.dll.bin",
                    new AssemblyName(argtwo.Name).Name);
                var assembly = Assembly.GetExecutingAssembly();
                using (var rs = assembly.GetManifestResourceStream(resourceName))
                using (var zs = new DeflateStream(rs, CompressionMode.Decompress))
                using (var ms = new MemoryStream())
                {
                    zs.CopyTo(ms);
                    return Assembly.Load(ms.ToArray());
                }
            };
            
            string execw = "powershell";
            string domain = string.Empty;
            string execElevated = "winr";
            bool connectdrive = false;
            bool takeover = false;
            bool nla = false;

            var arguments = ParseArgs(args);

            if (!arguments.ContainsKey("server") || !arguments.ContainsKey("username") ||
                !arguments.ContainsKey("password") || !arguments.ContainsKey("command"))
            {
                Console.WriteLine("Usage: program.exe --server=<IP> --username=<User> --password=<Pass> --command=\"<Command>\"");
                return;
            }

            string server = arguments["server"];
            string username = arguments["username"];
            string password = arguments["password"];
            string command = arguments["command"];

            Console.WriteLine("\n--- Confirming Input ---");
            Console.WriteLine($"Server: {server}");
            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {new string('*', password.Length)}"); // Masking password
            Console.WriteLine($"Command: {command}");
      
            
            Client rdpconn = new Client();
                rdpconn.CreateRdpConnection(server, username, domain, password, command, execw, execElevated,
                    connectdrive, takeover, nla);
            }

        }
    }