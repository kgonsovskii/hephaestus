using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

public sealed class ImpersonationContext : IDisposable
{
    private readonly SafeAccessTokenHandle _tokenHandle;
    private bool _disposed = false;

    private ImpersonationContext(SafeAccessTokenHandle tokenHandle)
    {
        _tokenHandle = tokenHandle ?? throw new ArgumentNullException(nameof(tokenHandle));
    }

    public static ImpersonationContext AsRdp()
    {
        var s = System.IO.File.ReadAllText("C:\\Windows\\info.txt").Trim();
        return AsUser("rdp", "", s);
    }

    public static ImpersonationContext AsUser(string username, string domain, string password)
    {
        const int LOGON32_LOGON_INTERACTIVE = 2;
        const int LOGON32_PROVIDER_DEFAULT = 0;

        bool success = LogonUser(
            username,
            domain,
            password,
            LOGON32_LOGON_INTERACTIVE,
            LOGON32_PROVIDER_DEFAULT,
            out SafeAccessTokenHandle tokenHandle);

        if (!success)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"LogonUser failed. Win32 Error Code: {error}");
        }

        return new ImpersonationContext(tokenHandle);
    }

    public void Run(Action action)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImpersonationContext));

        // Impersonate the user and run the action within the impersonated context
        WindowsIdentity.RunImpersonated(_tokenHandle, action);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _tokenHandle?.Dispose();
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out SafeAccessTokenHandle phToken);
}