using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Util
{
    static class NativeMethods
    {
        #region user32.dll

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsZoomed(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint MonitorFromWindow(nint hwnd, MonitorFromWindowFlags dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

        #endregion user32.dll

        #region shell32.dll

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern nint SHAppBarMessage(ABM dwMessage, [In] ref APPBARDATA pData);

        #endregion shell32.dll

        #region winmm.dl

        [DllImport("winmm.dll", EntryPoint = "timeSetEvent")]
        public static extern uint TimeSetEvent(uint uDelay, uint uResolution, TimeProc lpTimeProc, nint dwUser, FuEvent fuEvent);

        [DllImport("winmm.dll", EntryPoint = "timeKillEvent")]
        public static extern uint TimeKillEvent(uint uTimerID);

        #endregion winmm.dl
    }

    delegate void TimeProc(uint uTimerID, uint uMsg, nint dwUser, nint dw1, nint dw2);

    enum WM : uint
    {
        WM_GETMINMAXINFO = 0x0024,
        WM_NCCALCSIZE = 0x0083
    }

    [Flags]
    enum SWP : uint
    {
        SWP_NOSIZE = 0x0001
    }

    [Flags]
    enum WVP
    {
        WVR_ALIGNTOP = 0x0010,
        WVR_ALIGNLEFT = 0x0020,
        WVR_ALIGNBOTTOM = 0x0040,
        WVR_ALIGNRIGHT = 0x0080,
        WVR_VALIDRECTS = 0x0400
    }

    enum ABM : uint
    {
        ABM_GETAUTOHIDEBAREX = 0x000B
    }

    enum ABE : uint
    {
        ABE_LEFT = 0,
        ABE_TOP = 1,
        ABE_RIGHT = 2,
        ABE_BOTTOM = 3
    }

    [Flags]
    public enum MonitorFromWindowFlags : uint
    {
        MONITOR_DEFAULTTOPRIMARY = 0x1,
        MONITOR_DEFAULTTONEAREST = 0x2
    }

    [Flags]
    enum FuEvent : uint
    {
        TIME_ONESHOT = 0x00,
        TIME_PERIODIC = 0x01,
        TIME_CALLBACK_FUNCTION = 0x00,
        TIME_CALLBACK_EVENT_SET = 0x10,
        TIME_CALLBACK_EVENT_PULSE = 0x20
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NCCALCSIZE_PARAMS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public RECT[] rgrc;
        public WINDOWPOS lppos;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPOS
    {
        public nint hwnd;
        public nint hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public SWP flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        public MONITORINFO()
        {
            cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA
    {
        public uint cbSize;
        public nint hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;

        public APPBARDATA()
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }
}
