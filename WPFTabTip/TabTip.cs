﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WPFTabTip
{
    public static class TabTip
    {
        private const string tabTipWindowClassName = "IPTip_Main_Window";
        private const string TabTipExecPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
        private const string TabTipRegistryKeyName = @"HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7";

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

        internal static void OpenAndStartPoolingForClosedEvent(TabTipAutomation.DockModeOptions dockMode)
        {
            Open(dockMode);
            StartPoolingForTabTipClosedEvent();
        }

        /// <summary>
        /// Open TabTip
        /// </summary>
        public static void Open(TabTipAutomation.DockModeOptions dockMode = TabTipAutomation.DockModeOptions.NoChange)
        {
            const string TabTipDockedKey = "EdgeTargetDockedState";
            const string TabTipProcessName = "TabTip";

            if (dockMode != TabTipAutomation.DockModeOptions.NoChange)
            {
                int currentDockValue = (int)(Registry.GetValue(TabTipRegistryKeyName, TabTipDockedKey, 1) ?? 1);
                int expectedDockValue = dockMode == TabTipAutomation.DockModeOptions.Docked ? 1 : 0;

                if (currentDockValue != expectedDockValue)
                {
                    Registry.SetValue(TabTipRegistryKeyName, TabTipDockedKey, expectedDockValue);
                    foreach (Process tabTipProcess in Process.GetProcessesByName(TabTipProcessName))
                        tabTipProcess.Kill();
                }
            }

            if (EnvironmentEx.GetOSVersion() == OSVersion.Win10)
                EnableTabTipOpenInDesctopModeOnWin10();

            Process.Start(TabTipExecPath);

            if (EnvironmentEx.GetOSVersion() == OSVersion.Win10)
                TrayIcon.Trigger();
        }

        private static void EnableTabTipOpenInDesctopModeOnWin10()
        {
            const string TabTipAutoInvokeKey = "EnableDesktopModeAutoInvoke";

            int EnableDesktopModeAutoInvoke = (int) (Registry.GetValue(TabTipRegistryKeyName, TabTipAutoInvokeKey, -1) ?? -1);
            if (EnableDesktopModeAutoInvoke != 1)
                Registry.SetValue(TabTipRegistryKeyName, TabTipAutoInvokeKey, 1);
        }

        /// <summary>
        /// Open TabTip in undocked state
        /// </summary>
        public static void OpenUndocked()
        {
            Open(TabTipAutomation.DockModeOptions.Undocked);
        }

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

        // ReSharper disable once UnusedMember.Local
        private static bool IsTabTipProcessRunning => GetTabTipWindowHandle() != IntPtr.Zero;

        /// <summary>
        /// Gets TabTip Window Rectangle
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static Rectangle GetTabTipRectangle()
        {
            if (TabTipClosed())
                return new Rectangle();

            return GetWouldBeTabTipRectangle();
        }

        private static Rectangle previousTabTipRectangle;

        /// <summary>
        /// Gets Window Rectangle which would be occupied by TabTip if TabTip was opened.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        internal static Rectangle GetWouldBeTabTipRectangle()
        {
            RECT rect;

            if (!GetWindowRect(new HandleRef(null, GetTabTipWindowHandle()), out rect))
            {
                if (previousTabTipRectangle.Equals(new Rectangle())) //in case TabTip was closed and previousTabTipRectangle was not set
                    Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(TryGetTabTipRectangleToСache);

                return previousTabTipRectangle;
            }

            Rectangle wouldBeTabTipRectangle = new Rectangle(x: rect.Left, y: rect.Top, width: rect.Right - rect.Left + 1, height: rect.Bottom - rect.Top + 1);
            previousTabTipRectangle = wouldBeTabTipRectangle;

            return wouldBeTabTipRectangle;
        }

        private static void TryGetTabTipRectangleToСache(Task task)
        {
            RECT rect;
            if (GetWindowRect(new HandleRef(null, GetTabTipWindowHandle()), out rect))
                previousTabTipRectangle = new Rectangle(x: rect.Left, y: rect.Top, width: rect.Right - rect.Left + 1, height: rect.Bottom - rect.Top + 1);
        }
    }
}
