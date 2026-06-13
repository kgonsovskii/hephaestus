using System.Runtime.InteropServices;

namespace cp;

internal static class LinuxPamAuthentication
{
    private const int PamSuccess = 0;
    private const int PamPromptEchoOff = 1;
    private const int PamAuthErr = 7;

    [StructLayout(LayoutKind.Sequential)]
    private struct PamMessage
    {
        public int msg_style;
        public IntPtr msg;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PamResponse
    {
        public IntPtr resp;
        public int resp_retcode;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PamConvDelegate(int num_msg, IntPtr msg, IntPtr resp, IntPtr appdata_ptr);

    [StructLayout(LayoutKind.Sequential)]
    private struct PamConv
    {
        public PamConvDelegate conv;
        public IntPtr appdata_ptr;
    }

    [DllImport("libpam.so.0", EntryPoint = "pam_start")]
    private static extern int pam_start(
        [MarshalAs(UnmanagedType.LPStr)] string service_name,
        [MarshalAs(UnmanagedType.LPStr)] string user,
        ref PamConv conv,
        out IntPtr pamh);

    [DllImport("libpam.so.0", EntryPoint = "pam_authenticate")]
    private static extern int pam_authenticate(IntPtr pamh, int flags);

    [DllImport("libpam.so.0", EntryPoint = "pam_end")]
    private static extern int pam_end(IntPtr pamh, int ret);

    [ThreadStatic]
    private static string? _password;

    public static bool TryAuthenticate(string username, string password, out string msg)
    {
        msg = "";
        _password = password;
        IntPtr pamh = IntPtr.Zero;

        try
        {
            var conv = new PamConv
            {
                conv = Conversation,
                appdata_ptr = IntPtr.Zero
            };

            var rc = pam_start("login", username, ref conv, out pamh);
            if (rc != PamSuccess)
            {
                msg = $"pam_start failed ({rc}).";
                return false;
            }

            rc = pam_authenticate(pamh, 0);
            if (rc == PamSuccess)
                return true;

            msg = rc == PamAuthErr
                ? "The username or password is incorrect."
                : $"pam_authenticate failed ({rc}).";
            return false;
        }
        catch (DllNotFoundException)
        {
            msg = "libpam.so.0 not found (install libpam0g on the host).";
            return false;
        }
        finally
        {
            _password = null;
            if (pamh != IntPtr.Zero)
                pam_end(pamh, 0);
        }
    }

    private static int Conversation(int num_msg, IntPtr msg, IntPtr resp, IntPtr appdata_ptr)
    {
        var responseArray = Marshal.AllocHGlobal(Marshal.SizeOf<PamResponse>() * num_msg);
        try
        {
            for (var i = 0; i < num_msg; i++)
            {
                var messagePtr = Marshal.ReadIntPtr(msg, i * IntPtr.Size);
                var message = Marshal.PtrToStructure<PamMessage>(messagePtr);

                IntPtr responseText = IntPtr.Zero;
                if (message.msg_style == PamPromptEchoOff)
                    responseText = Marshal.StringToHGlobalAnsi(_password ?? "");

                var response = new PamResponse
                {
                    resp = responseText,
                    resp_retcode = 0
                };
                Marshal.StructureToPtr(response, responseArray + (i * Marshal.SizeOf<PamResponse>()), false);
            }

            Marshal.WriteIntPtr(resp, responseArray);
            responseArray = IntPtr.Zero;
            return PamSuccess;
        }
        finally
        {
            if (responseArray != IntPtr.Zero)
                Marshal.FreeHGlobal(responseArray);
        }
    }
}
