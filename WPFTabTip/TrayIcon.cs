using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WPFTabTip
{
    internal static class TrayIcon
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string sClassName, string sAppName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public static void Trigger()
        {
            var nullIntPtr = IntPtr.Zero;
            var trayWnd = FindWindow("Shell_TrayWnd", null);

            if (trayWnd != nullIntPtr)
            {
                var trayNotifyWnd = FindWindowEx(trayWnd, nullIntPtr, "TrayNotifyWnd", null);
                if (trayNotifyWnd != nullIntPtr)
                {
                    var tIPBandWnd = FindWindowEx(trayNotifyWnd, nullIntPtr, "TIPBand", null);

                    if (tIPBandWnd != nullIntPtr)
                    {
                        PostMessage(tIPBandWnd, (UInt32)WMessages.WM_LBUTTONDOWN, 1, 65537);
                        PostMessage(tIPBandWnd, (UInt32)WMessages.WM_LBUTTONUP, 1, 65537);
                    }
                }
            }
        }

        public enum WMessages : int
        {
            WM_LBUTTONDOWN = 0x201,
            WM_LBUTTONUP = 0x202,
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 0x101,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14,
        }
    }
}
