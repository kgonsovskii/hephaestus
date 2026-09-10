using System.Text;

namespace InstallRemote;

public static class RemoteInstallCredsFile
{
    public const string DefaultFileName = "install-remote-creds.txt";

    public static RemoteCreds LoadFromPathOrThrow(string path) =>
        LoadAllFromPathOrThrow(path)[0];

    public static IReadOnlyList<RemoteCreds> LoadAllFromPathOrThrow(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Expected {DefaultFileName} with one or more host/login/password/profile quadruplets. Path: {path}",
                path);

        var lines = File.ReadAllText(path, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        var taken = new List<string>();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length == 0)
                continue;
            if (t.StartsWith('#'))
                continue;
            taken.Add(t);
        }

        if (taken.Count == 0 || taken.Count % 4 != 0)
            throw new InvalidOperationException(
                $"{path} must contain one or more quadruplets of non-empty, non-comment lines: SSH host, login, password, profile (got {taken.Count} line(s)).");

        var list = new List<RemoteCreds>(taken.Count / 4);
        for (var i = 0; i < taken.Count; i += 4)
        {
            list.Add(new RemoteCreds(
                taken[i],
                taken[i + 1],
                taken[i + 2],
                ValidateProfileName(taken[i + 3])));
        }

        return list;
    }

    public static string ValidateProfileName(string value)
    {
        var profile = value.Trim().Trim('\\', '/');
        if (string.IsNullOrWhiteSpace(profile) || profile is "." or ".."
            || profile.Contains('/') || profile.Contains('\\'))
        {
            throw new ArgumentException($"Invalid profile name: '{value}'");
        }

        return profile;
    }

    public static string ResolveCredsPath(string baseDirectory, string fileName = DefaultFileName)
    {
        var besideExe = Path.Combine(baseDirectory, fileName);
        if (File.Exists(besideExe))
            return besideExe;

        var fromRepoShared = Path.Combine(Environment.CurrentDirectory, "install", "shared", fileName);
        if (File.Exists(fromRepoShared))
            return fromRepoShared;

        var fromRepoInstall = Path.Combine(Environment.CurrentDirectory, "install", fileName);
        if (File.Exists(fromRepoInstall))
            return fromRepoInstall;

        return besideExe;
    }
}

public sealed record RemoteCreds(string Server, string Login, string Password, string Profile);
