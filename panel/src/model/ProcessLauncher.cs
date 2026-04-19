using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace model;

public class ProcessLauncher
{
    [StructLayout(LayoutKind.Sequential)]
    private class SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor = IntPtr.Zero;
        public bool bInheritHandle = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(
        out SafeFileHandle hReadPipe,
        out SafeFileHandle hWritePipe,
        ref SECURITY_ATTRIBUTES lpPipeAttributes,
        int nSize);

    public static bool LaunchAsDesktopUser(string exePath, string arguments, Action<string> logger)
    {
        uint sessionId = WTSGetActiveConsoleSessionId();
        if (!WTSQueryUserToken(sessionId, out IntPtr userToken))
        {
            logger?.Invoke("❌ Failed to get active session token.");
            return false;
        }

        const uint TOKEN_ALL_ACCESS = 0xF01FF;
        const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400 | 0x00000010; // CREATE_NEW_CONSOLE

        if (!DuplicateTokenEx(userToken, TOKEN_ALL_ACCESS, IntPtr.Zero, 2, 1, out IntPtr duplicatedToken))
        {
            logger?.Invoke("❌ Failed to duplicate token.");
            return false;
        }

        if (!CreateEnvironmentBlock(out IntPtr environment, duplicatedToken, false))
        {
            logger?.Invoke("❌ Failed to create environment block.");
            return false;
        }

        // Set up pipe for output
        var security = new SECURITY_ATTRIBUTES
        {
            nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            bInheritHandle = true
        };

        CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, ref security, 0);

        var si = new STARTUPINFO
        {
            cb = Marshal.SizeOf<STARTUPINFO>(),
            lpDesktop = @"winsta0\default",
            hStdOutput = writeHandle.DangerousGetHandle(),
            hStdError = writeHandle.DangerousGetHandle(),
            dwFlags = 0x00000100 // STARTF_USESTDHANDLES
        };

        var pi = new PROCESS_INFORMATION();

        bool success = CreateProcessAsUser(
            duplicatedToken,
            null,
            $"\"{exePath}\" {arguments}",
            IntPtr.Zero,
            IntPtr.Zero,
            true, // inherit handles
            CREATE_UNICODE_ENVIRONMENT,
            environment,
            null,
            ref si,
            out pi
        );

        writeHandle.Close(); // Parent doesn't need write end

        if (!success)
        {
            logger?.Invoke("❌ Failed to launch process.");
            return false;
        }

        // Log output in background
        Task.Run(() =>
        {
            try
            {
                using var reader = new StreamReader(new FileStream(readHandle, FileAccess.Read, 4096, false), Encoding.Default);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    logger?.Invoke(line);
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke("❌ Exception reading output: " + ex.Message);
            }
        });

        logger?.Invoke($"✅ Process launched with PID: {pi.dwProcessId}");
        return true;
    }
}