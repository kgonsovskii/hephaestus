using System;
using System.Runtime.InteropServices;

namespace SharpRDP
{
    class KeyboardLayoutSetter
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        const uint KLF_ACTIVATE = 0x00000001;

        public static void SetEnglishUSKeyboardLayout()
        {
            try
            {
                // "00000409" is the KLID for English (United States)
                IntPtr hkl = LoadKeyboardLayout("00000409", KLF_ACTIVATE);
                if (hkl == IntPtr.Zero)
                {
                    Program.Log("X Failed to load keyboard layout.");
                }
                else
                {
                    ActivateKeyboardLayout(hkl, KLF_ACTIVATE);
                    Program.Log("X Keyboard layout set to English (US).");
                }
            }
            catch (Exception e)
            {
                Program.Log(e.Message);
            }
        }
    }
}