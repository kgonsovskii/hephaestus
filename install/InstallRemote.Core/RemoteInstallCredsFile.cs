using System.Text;

namespace InstallRemote;

public static class RemoteInstallCredsFile
{
    public const string DefaultFileName = "install-remote-creds.txt";

    public static RemoteCreds LoadFromPathOrThrow(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Expected {DefaultFileName} with three lines (host, login, password). Path: {path}",
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
            if (taken.Count == 3)
                break;
        }

        if (taken.Count < 3)
            throw new InvalidOperationException(
                $"{path} must contain three non-empty, non-comment lines: SSH host, login, password (got {taken.Count}).");

        return new RemoteCreds(taken[0], taken[1], taken[2]);
    }

    public static string ResolveCredsPath(string baseDirectory, string fileName = DefaultFileName)
    {
        var besideExe = Path.Combine(baseDirectory, fileName);
        if (File.Exists(besideExe))
            return besideExe;

        var fromRepoInstall = Path.Combine(Environment.CurrentDirectory, "install", fileName);
        if (File.Exists(fromRepoInstall))
            return fromRepoInstall;

        return besideExe;
    }
}

public sealed record RemoteCreds(string Server, string Login, string Password);
