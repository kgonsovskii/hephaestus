using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.NetBios;

namespace cp;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("Performance", "CA1823:Avoid unused private fields")]
public class RemoteAuthentication
{
    // Import necessary Windows API functions for user logon
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public extern static bool CloseHandle(IntPtr handle);

    // Logon type and provider constants
    private const int LOGON32_LOGON_INTERACTIVE = 2;
    private const int LOGON32_LOGON_NETWORK = 3; // For network logon
    private const int LOGON32_LOGON_SERVICE = 5; // For service logon (if needed)
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    public static bool IsValidUser(string username, string password, string serverIpAddress, out string msg)
    {
        #if DEBUG
            msg = "debug mode";
            return true;
#endif
        IntPtr userToken = IntPtr.Zero;

        try
        {
            bool isSuccess = LogonUser(
                username,
                serverIpAddress,
                password,
                LOGON32_LOGON_NETWORK,
                LOGON32_PROVIDER_DEFAULT,
                out userToken);

            if (!isSuccess)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine("Logon failed with error code: " + errorCode);
                msg = HandleErrorCode(errorCode);
                return false;
            }

            msg = "";
            return isSuccess;
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            return false;
        }
        finally
        {
            if (userToken != IntPtr.Zero)
            {
                CloseHandle(userToken);
            }
        }
    }

    private static string HandleErrorCode(int errorCode)
    {
        switch (errorCode)
        {
            case 1326:
                return ("ERROR_LOGON_FAILURE: The username or password is incorrect.");
                break;
            case 1314:
                return ("ERROR_PRIVILEGE_NOT_HELD: The user does not have the required privilege.");
                break;
            case 5:
                return ("ERROR_ACCESS_DENIED: Access is denied.");
                break;
            case 86:
                return ("ERROR_INVALID_PASSWORD: The password is incorrect.");
                break;
            default:
                return ($"Unknown error: {errorCode}");
                break;
        }
    }
}
