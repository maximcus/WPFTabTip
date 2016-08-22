using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WPFTabTip
{
    internal class Screen
    {

        public Rectangle Bounds { get; }

        public Screen(Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).EnsureHandle();
            IntPtr monitor = NativeMethods.MonitorFromWindow(windowHandle, NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.NativeMonitorInfo monitorInfo = new NativeMethods.NativeMonitorInfo();
            NativeMethods.GetMonitorInfo(monitor, monitorInfo);

            Bounds = Rectangle.FromLTRB(monitorInfo.Monitor.Left, monitorInfo.Monitor.Top, monitorInfo.Monitor.Right, monitorInfo.Monitor.Bottom);
        }

        private static class NativeMethods
        {
            public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;


            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);


            [DllImport("user32.dll")]
            public static extern bool GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);


            [Serializable, StructLayout(LayoutKind.Sequential)]
            public struct NativeRectangle
            {
                public readonly int Left;
                public readonly int Top;
                public readonly int Right;
                public readonly int Bottom;
            }

#pragma warning disable 169

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public sealed class NativeMonitorInfo
            {
                // ReSharper disable once UnusedMember.Local
                public Int32 Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
#pragma warning disable 649
                public NativeRectangle Monitor;
#pragma warning restore 649
                public NativeRectangle Work;
                public Int32 Flags;
            }
#pragma warning restore 169

        }
    }
}
