using System;
using System.IO.Compression;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;

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
        
        private static string tagFile = "C:\\install\\tag_local.txt";
        static public bool completed = false;
        private static Thread WaithThread;
        public static string tag;

        static void Main(string[] args)
        {
            KeyboardLayoutSetter.SetEnglishUSKeyboardLayout();
            AppDomain.CurrentDomain.AssemblyResolve += (sender, argtwo) =>
            {
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
                Console.WriteLine(
                    "Usage: program.exe --server=<IP> --username=<User> --password=<Pass> --command=\"<Command>\"");
                return;
            }

            string server = arguments["server"];
            string username = arguments["username"];
            string password = arguments["password"];
            string command = arguments["command"];
            string timeout = arguments["timeout"];
            tag = arguments["tag"];
            
            Console.WriteLine("\n--- Confirming Input ---");
            Console.WriteLine($"Server: {server}");
            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {new string('*', password.Length)}"); // Masking password
            Console.WriteLine($"Command: {command}");
            Console.WriteLine($"Timeout: {timeout}");

            var timeOutMs = 1000 * 60 * 30;
            if (!string.IsNullOrEmpty(timeout))
            {
                timeOutMs = int.Parse(timeout) * 1000;

            }
            
            if (System.IO.File.Exists(tagFile))
                System.IO.File.Delete(tagFile);

            Watch(timeOutMs);

            try
            {
                Client rdpconn = new Client();
                Console.WriteLine("Run RDP");
                rdpconn.CreateRdpConnection(server, username, domain, password, command, execw, execElevated,
                    connectdrive, takeover, nla);
                completed = true;
                Console.WriteLine("RDP completed");
                Thread.Sleep(300);
                Report();
                WaithThread.Abort();
            }
            catch (Exception e)
            {
            }
            Report();
        }
        
        public static void Report()
        {
            Console.WriteLine($"Reprint local tag: {tag}, {completed}");
            if (!completed)
            {
                System.IO.File.WriteAllText(tagFile, tag + " timeout");
                Console.WriteLine("RDP connection thread completed with error.");
            }
            else
            {
                System.IO.File.WriteAllText(tagFile, tag + " ok");
                Console.WriteLine("RDP connection completed successfully.");
            }
        }



        static void Watch(int timeoutMs)
        {
            WaithThread = new Thread(() =>
            {
                var starttime = Environment.TickCount;
                while (!completed)
                {
                    Thread.Sleep(50);
                    if (Environment.TickCount - starttime > timeoutMs)
                        break;
                }
                Report();
                Environment.Exit(0);
            });
            WaithThread.Start();
        }
    }
}