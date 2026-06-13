using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace cp;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class WindowsLogonAuthentication
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private const int LOGON32_LOGON_NETWORK = 3;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    public static bool TryAuthenticate(string username, string password, out string msg)
    {
        msg = "";
        var (user, domain) = SplitUserDomain(username.Trim());

        IntPtr userToken = IntPtr.Zero;
        try
        {
            if (!LogonUser(user, domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out userToken))
            {
                msg = DescribeWin32Error(Marshal.GetLastWin32Error());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            return false;
        }
        finally
        {
            if (userToken != IntPtr.Zero)
                CloseHandle(userToken);
        }
    }

    private static (string User, string Domain) SplitUserDomain(string username)
    {
        var slash = username.IndexOf('\\');
        if (slash >= 0)
            return (username[(slash + 1)..], username[..slash]);

        var at = username.IndexOf('@');
        if (at >= 0)
            return (username[..at], username[(at + 1)..]);

        return (username, ".");
    }

    private static string DescribeWin32Error(int errorCode) =>
        errorCode switch
        {
            1326 => "The username or password is incorrect.",
            1314 => "The user does not have the required privilege.",
            5 => "Access is denied.",
            86 => "The password is incorrect.",
            _ => $"Logon failed (Win32 error {errorCode})."
        };
}
