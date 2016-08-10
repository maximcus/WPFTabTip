using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WPFTabTip
{
    public static class TabTip
    {
        private const string tabTipWindowClassName = "IPTip_Main_Window";
        private const string TabTipExecPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(String sClassName, String sAppName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public readonly int Left;        // x position of upper-left corner
            public readonly int Top;         // y position of upper-left corner
            public readonly int Right;       // x position of lower-right corner
            public readonly int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Specifies we wish to retrieve window styles.
        /// </summary>
        private const int GWL_STYLE = -16;

        private const uint KeyboardClosedStyle = 2617245696;

        /// <summary>
        /// Signals that TabTip was closed
        /// </summary>
        public static event Action Closed;

        private static IntPtr GetTabTipWindowHandle() => FindWindow(tabTipWindowClassName, null);


        /// <summary>
        /// Open TabTip
        /// </summary>
        public static void Open()
        {
            Process.Start(TabTipExecPath);
            StartTimerForTabTipClosedEvent();
        }

        /// <summary>
        /// Close TabTip
        /// </summary>
        public static void Close()
        {
            const uint WM_SYSCOMMAND = 274;
            const uint SC_CLOSE = 61536;
            PostMessage(GetTabTipWindowHandle().ToInt32(), WM_SYSCOMMAND, (int)SC_CLOSE, 0);
        }

        private static void StartTimerForTabTipClosedEvent()
        {
            PoolingTimer.Start(() =>
            {
                IntPtr KeyboardWnd = GetTabTipWindowHandle();
                if (KeyboardWnd.ToInt32() == 0 || GetWindowLong(KeyboardWnd, GWL_STYLE) == KeyboardClosedStyle)
                {
                    Closed?.Invoke();
                    return true;
                }

                return false;
            }, 
            dueTime: TimeSpan.FromMilliseconds(700), 
            period: TimeSpan.FromMilliseconds(50));
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static Rectangle GetTabTipRectangle()
        {
            RECT rect;
            
            if (!GetWindowRect(new HandleRef(null, GetTabTipWindowHandle()), out rect))
                return new Rectangle();

            return new Rectangle(x: rect.Left, y: rect.Top, width: rect.Right - rect.Left + 1, height: rect.Bottom - rect.Top + 1);
        }
    }
}
