using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Commons;

namespace model;

public static class ServerModelLoader
{
    public static JsonSerializerOptions JSO = new()
        { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static ServerModel LoadServer(string serverName)
    {
        try
        {
            var server = LoadServerFile(DataFile(serverName));
            return server;
        }
        catch (Exception e)
        {
            Dev.DefaultServer(serverName);
            return LoadServer(serverName);
        }
    }

    public static ServerModel LoadServerFile(string serverFile)
    {
        var server = LoadServerFileInternal(serverFile);
        server.Refresh();
        return server;
    }

    public static ServerModel LoadServerFileInternal(string serverFile)
    {
        var server = JsonSerializer.Deserialize<ServerModel>(File.ReadAllText(serverFile), JSO)!;
        return server;
    }

    public static void SaveServerFile(string serverFile, ServerModel server)
    {
        File.WriteAllText(serverFile,
            JsonSerializer.Serialize(server, JSO));
    }

    public static void SaveServer(string serverName, ServerModel server)
    {
        SaveServerFile(DataFile(serverName), server);
    }

    public static string ServerDir(string serverName)
    {
        return Path.Combine(RootDataStatic, serverName);
    }

    internal static string DataFile(string serverName)
    {
        return Path.Combine(ServerDir(serverName), "server.json");
    }

    public static string SourceCertDirStatic => PathResolver.CertDirectory(HephaestusDataRootStatic);

    private static IHephaestusPathResolver? _pathResolver;

    private static IHephaestusPathResolver PathResolver =>
        _pathResolver ??= CreatePathResolver();

    private static IHephaestusPathResolver CreatePathResolver()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dir))
            throw new InvalidOperationException("Could not resolve assembly directory for Hephaestus path options.");
        return HephaestusPathResolver.FromAppSettingsInDirectory(dir);
    }

    private static string? _hephaestusDataRootStatic;

    public static string HephaestusDataRootStatic
    {
        get
        {
            if (_hephaestusDataRootStatic is null)
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir))
                    throw new InvalidOperationException("Could not resolve assembly directory for Hephaestus data root.");
                _hephaestusDataRootStatic = PathResolver.ResolveHephaestusDataRoot(dir);
            }

            return _hephaestusDataRootStatic;
        }
    }

    private static string? _rootDirStatic;

    public static string RootDirStatic
    {
        get
        {
            if (_rootDirStatic is null)
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir))
                    throw new InvalidOperationException("Could not resolve assembly directory for repository root.");
                _rootDirStatic = PathResolver.ResolveRepositoryRoot(dir);
            }

            return _rootDirStatic;
        }
    }

    public static string RootDataStatic
    {
        get
        {
            return @"C:\data";
        }
    }

    public static string TroyanBuilder
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "output","TroyanBuilder.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "TroyanBuilder.exe");
            return result;
        }
    }

    public static string Refiner
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "output","refiner.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "refiner.exe");
            return result;
        }
    }

    public static string Packer
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "output","packer.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "packer.exe");
            return result;
        }
    }

    public static string CertTool
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "output","certtool.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "certtool.exe");
            return result;
        }
    }

    public static string UserDataDir(string server)
    {
        if (server == null)
            server = "";
        return Path.Combine(RootDataStatic,server);
    }

    public const string BodyFileConst = "body.txt";

    public static string UserDataFile(string server, string file)
    {
        return Path.Combine(UserDataDir(server), file);
    }

    public static string UserDataBody(string server)
    {
        return UserDataFile(server, BodyFileConst);
    }

    public static string CpDirStatic => Path.Combine(RootDirStatic, "cp");

    public static string AdsDirStatic => Path.Combine(RootDirStatic, "ads");

    public static string PhpDirStatic => Path.Combine(RootDirStatic, "php");

    public static string CertDirStatic => Path.Combine(RootDirStatic, "cert");

    public static string SysDirStatic => Path.Combine(RootDirStatic, "sys");

    public static string TroyanDirStatic => Path.Combine(RootDirStatic, "troyan");

    public static string TroyanScriptDirStatic => Path.Combine(TroyanDirStatic, "troyanps");

    public static string TroyanVbsDirStatic => Path.Combine(TroyanDirStatic, "troyanvbs");
}
