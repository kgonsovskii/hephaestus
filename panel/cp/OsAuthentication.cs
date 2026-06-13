namespace cp;

/// <summary>Validates CP login against the host OS account (Windows LogonUser / Linux PAM).</summary>
public static class OsAuthentication
{
    public static bool IsValidUser(string username, string password, out string msg)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            msg = "Username and password are required.";
            return false;
        }

        if (OperatingSystem.IsWindows())
            return WindowsLogonAuthentication.TryAuthenticate(username, password, out msg);

        if (OperatingSystem.IsLinux())
            return LinuxPamAuthentication.TryAuthenticate(username, password, out msg);

        msg = "OS authentication is not supported on this platform.";
        return false;
    }
}
