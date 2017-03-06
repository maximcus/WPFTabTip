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
            IntPtr windowHandle = window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;

            IntPtr monitor = window != null ? NativeMethods.MonitorFromWindow(windowHandle, NativeMethods.MONITOR_DEFAULTTONEAREST) : NativeMethods.MonitorFromPoint(new NativeMethods.POINT(0, 0), NativeMethods.MonitorOptions.MONITOR_DEFAULTTOPRIMARY);

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

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                }

                public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

                public static implicit operator System.Drawing.Point(POINT p)
                {
                    return new System.Drawing.Point(p.X, p.Y);
                }

                public static implicit operator POINT(System.Drawing.Point p)
                {
                    return new POINT(p.X, p.Y);
                }
            }

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

            internal enum MonitorOptions : uint
            {
                MONITOR_DEFAULTTONULL = 0x00000000,
                MONITOR_DEFAULTTOPRIMARY = 0x00000001,
                MONITOR_DEFAULTTONEAREST = 0x00000002
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
