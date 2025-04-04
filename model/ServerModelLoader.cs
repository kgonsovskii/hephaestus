﻿using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

namespace model;

public static class ServerModelLoader
{
       
    public static string SourceCertDirStatic
    {
        get
        {
            return  @"C:\soft\hephaestus\cert";
        }
    }
        
    private static string? _rootDirStatic = null;

    public static string RootDirStatic
    {
        get
        {
            if (_rootDirStatic == null)
            {
                var found = false;
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                while (!found)
                {
                    var name = System.IO.Path.GetFileName(dir);

                    if (name.ToLower(CultureInfo.InvariantCulture) == "cloner" || name.ToLower(CultureInfo.InvariantCulture) == "foregrounder" || name.ToLower(CultureInfo.InvariantCulture) == "troyanbuilder" || name.ToLower(CultureInfo.InvariantCulture) == "cp" || name.ToLower(CultureInfo.InvariantCulture) == "refiner")
                    {
                        dir = Directory.GetParent(dir)?.FullName;
                        _rootDirStatic = dir;
                        break;
                    }

                    dir = Directory.GetParent(dir)?.FullName;
                }
            }
            return _rootDirStatic!;
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
            var result = Path.Combine(RootDirStatic, "troyanbuilder","bin/debug/net9.0/TroyanBuilder.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "TroyanBuilder.exe");
            return result;
        }
    }
    
    public static string Refiner
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "refiner","bin/debug/net9.0/refiner.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "refiner.exe");
            return result;
        }
    }
    
    public static string Cloner
    {
        get
        {
            var result = Path.Combine(RootDirStatic, "cloner","bin/debug/net9.0/cloner.exe");
            if (!File.Exists(result))
                result = Path.Combine(CpDirStatic, "cloner.exe");
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