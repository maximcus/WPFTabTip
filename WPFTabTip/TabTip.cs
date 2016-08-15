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
        
        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

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
        private static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Signals that TabTip was closed after it was opened 
        /// with a call to StartPoolingForTabTipClosedEvent method
        /// </summary>
        internal static event Action Closed;

        private static IntPtr GetTabTipWindowHandle() => FindWindow(tabTipWindowClassName, null);
        
        internal static void OpenAndStartPoolingForClosedEvent()
        {
            Process.Start(TabTipExecPath);
            StartPoolingForTabTipClosedEvent();
        }

        /// <summary>
        /// Open TabTip
        /// </summary>
        public static void Open() => Process.Start(TabTipExecPath);

        /// <summary>
        /// Close TabTip
        /// </summary>
        public static void Close()
        {
            const int WM_SYSCOMMAND = 274;
            const int SC_CLOSE = 61536;
            SendMessage(GetTabTipWindowHandle().ToInt32(), WM_SYSCOMMAND, SC_CLOSE, 0);
        }

        private static void StartPoolingForTabTipClosedEvent()
        {
            PoolingTimer.PoolUntilTrue(
                PoolingFunc: TabTipClosed,
                Callback: () => Closed?.Invoke(),
                dueTime: TimeSpan.FromMilliseconds(700),
                period: TimeSpan.FromMilliseconds(50));
        }

        private static bool TabTipClosed()
        {
            const int GWL_STYLE = -16; // Specifies we wish to retrieve window styles.
            const uint KeyboardClosedStyle = 2617245696;
            IntPtr KeyboardWnd = GetTabTipWindowHandle();
            return (KeyboardWnd.ToInt32() == 0 || GetWindowLong(KeyboardWnd, GWL_STYLE) == KeyboardClosedStyle);
        }

        /// <summary>
        /// Gets TabTip Window Rectangle
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static Rectangle GetTabTipRectangle()
        {
            if (TabTipClosed())
                return new Rectangle();

            RECT rect;
            
            if (!GetWindowRect(new HandleRef(null, GetTabTipWindowHandle()), out rect))
                return new Rectangle();

            return new Rectangle(x: rect.Left, y: rect.Top, width: rect.Right - rect.Left + 1, height: rect.Bottom - rect.Top + 1);
        }
    }
}
