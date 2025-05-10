using System.Runtime.InteropServices;

public static class RdpSessionHelper
{
    private const int WTS_CURRENT_SERVER_HANDLE = 0;
    private const int WTSActive = 0;

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        out IntPtr ppSessionInfo,
        out int pCount
    );

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WTS_INFO_CLASS wtsInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned
    );

    private enum WTS_INFO_CLASS
    {
        WTSUserName = 5,
        WTSConnectState = 8
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pWinStationName;
        public int State;
    }

    /// <summary>
    /// Returns the active RDP session ID for the given username, or -1 if none found.
    /// </summary>
    public static int GetActiveRdpSessionIdForUser(string username)
    {
        IntPtr pSessionInfo = IntPtr.Zero;
        int sessionCount = 0;

        if (WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessionInfo, out sessionCount))
        {
            int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            IntPtr current = pSessionInfo;

            for (int i = 0; i < sessionCount; i++)
            {
                WTS_SESSION_INFO sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFO>(current);
                current += dataSize;

                if (sessionInfo.State != WTSActive)
                    continue;

                if (WTSQuerySessionInformation(IntPtr.Zero, sessionInfo.SessionID, WTS_INFO_CLASS.WTSUserName, out IntPtr buffer, out int strLen) && strLen > 1)
                {
                    string user = Marshal.PtrToStringAnsi(buffer);
                    WTSFreeMemory(buffer);

                    if (user.Equals(username, StringComparison.OrdinalIgnoreCase))
                    {
                        WTSFreeMemory(pSessionInfo);
                        return sessionInfo.SessionID;
                    }
                }
            }

            WTSFreeMemory(pSessionInfo);
        }

        return -1; // Not found
    }
}
