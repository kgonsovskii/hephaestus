using System.Net;

namespace model;

/// <summary>Validates clone remote-install inputs.</summary>
public static class CloneRemoteInstallTarget
{
    /// <summary>Returns a user-facing error message, or <c>null</c> if <paramref name="profileRaw"/> is allowed for clone.</summary>
    public static string? ValidateProfile(string? profileRaw)
    {
        if (string.IsNullOrWhiteSpace(profileRaw))
            return "Profile is required.";

        var profile = profileRaw.Trim().Trim('/', '\\');
        if (profile.Length == 0 || profile is "." or ".."
            || profile.Contains('/') || profile.Contains('\\'))
            return $"Invalid profile name: '{profileRaw}'";

        if (string.Equals(profile, PanelServerIdentity.DefaultKey, StringComparison.OrdinalIgnoreCase))
            return "Profile name 'default' is reserved; choose another name for remote clone.";

        return null;
    }

    /// <summary>Returns a user-facing error message, or <c>null</c> if <paramref name="hostRaw"/> is allowed.</summary>
    public static string? ValidateHost(string? hostRaw)
    {
        var host = hostRaw?.Trim() ?? "";
        if (host.Length == 0)
            return "Remote server host is required.";

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return "Remote server cannot be localhost.";

        if (string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::", StringComparison.OrdinalIgnoreCase))
            return "Remote server cannot use that address.";

        if (IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip))
            return "Remote server cannot be a loopback address.";

        return null;
    }
}
