using System.Runtime.Versioning;

namespace InstallRemote;

/// <summary>Resolves <c>sshpass</c> on PATH, or on Windows runs the same discovery/download path as the install-remote tool.</summary>
public static class SshPassBootstrap
{
    /// <summary>Returns full path to sshpass executable.</summary>
    /// <param name="logInfo">Optional progress (Windows bootstrap); e.g. <c>Console.WriteLine</c> from CLI.</param>
    public static Task<string> EnsureAsync(
        Action<string>? logInfo = null,
        CancellationToken cancellationToken = default)
    {
        var found = RemoteInstallRunner.FindSshPassOnPath();
        if (found != null)
            return Task.FromResult(found);

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromException<string>(
                new InvalidOperationException("sshpass not found on PATH (e.g. apt install sshpass)."));
        }

        return EnsureWindowsAsync(logInfo, cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    private static Task<string> EnsureWindowsAsync(Action<string>? logInfo, CancellationToken cancellationToken) =>
        WindowsSshPassBootstrap.EnsureAsync(logInfo, cancellationToken);
}
