using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace NiVE3.Util
{
    static class MonitorDimension
    {
        // SEE: https://mntone.hateblo.jp/entry/2020/08/02/111309

        public const int HideAppBarSpace = 2;

        public static WindowMaxRect CalcMaxRectFromWindow(Window window)
        {
            return CalcMaxRectFromWindow(new WindowInteropHelper(window).Handle);
        }

        public static WindowMaxRect CalcMaxRectFromWindow(nint hwnd)
        {
            var hMonitor = NativeMethods.MonitorFromWindow(hwnd, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
            {
                return WindowMaxRect.Empty;
            }

            var monitorInfo = new MONITORINFO();
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                return WindowMaxRect.Empty;
            }

            var rcWork = monitorInfo.rcWork;
            var rcMonitor = monitorInfo.rcMonitor;

            var maxPositionX = Math.Abs(rcWork.Left - rcMonitor.Left);
            var maxPositionY = Math.Abs(rcWork.Top - rcMonitor.Top);
            var maxWidth = Math.Abs(rcWork.Right - rcWork.Left) + 2;
            var maxHeight = Math.Abs(rcWork.Bottom - rcWork.Top) + 2;
            var maxTrackWidth = maxWidth;
            var maxTrackHeight = maxHeight;

            switch (CheckHasAppBarAutoHide(rcMonitor))
            {
                case AppBarHideMode.Left:
                    maxPositionX += HideAppBarSpace;
                    maxTrackWidth -= HideAppBarSpace;
                    maxWidth -= HideAppBarSpace;
                    break;
                case AppBarHideMode.Top:
                    maxPositionY += HideAppBarSpace;
                    maxTrackHeight -= HideAppBarSpace;
                    maxHeight -= HideAppBarSpace;
                    break;
                case AppBarHideMode.Right:
                    maxTrackWidth -= HideAppBarSpace;
                    maxWidth -= HideAppBarSpace;
                    break;
                case AppBarHideMode.Bottom:
                    maxTrackHeight -= HideAppBarSpace;
                    maxHeight -= HideAppBarSpace;
                    break;
            }

            return new WindowMaxRect(maxPositionX, maxPositionY, maxWidth, maxHeight, maxTrackWidth, maxTrackHeight);
        }

        public static DpiScale GetMonitorDpiScale(Window window)
        {
            return GetMonitorDpiScale(new WindowInteropHelper(window).Handle);
        }

        public static DpiScale GetMonitorDpiScale(nint hwnd)
        {
            var hMonitor = NativeMethods.MonitorFromWindow(hwnd, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
            {
                return new DpiScale(1.0, 1.0);
            }

            NativeMethods.GetDpiForMonitor(hMonitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY);

            return new DpiScale(dpiX / 96.0, dpiY / 96.0);
        }

        public static AppBarHideMode CheckHasAppBarAutoHide(RECT rc)
        {
            if (HasAutoHideAppBar(rc, ABE.ABE_LEFT))
            {
                return AppBarHideMode.Left;
            }
            else if (HasAutoHideAppBar(rc, ABE.ABE_TOP))
            {
                return AppBarHideMode.Top;
            }
            else if (HasAutoHideAppBar(rc, ABE.ABE_RIGHT))
            {
                return AppBarHideMode.Right;
            }
            else if (HasAutoHideAppBar(rc, ABE.ABE_BOTTOM))
            {
                return AppBarHideMode.Bottom;
            }
            else
            {
                return AppBarHideMode.None;
            }
        }

        static bool HasAutoHideAppBar(RECT rc, ABE edge)
        {
            var data = new APPBARDATA
            {
                uEdge = (uint)edge,
                rc = rc
            };
            var hAppbar = NativeMethods.SHAppBarMessage(ABM.ABM_GETAUTOHIDEBAREX, ref data);
            return NativeMethods.IsWindow(hAppbar);
        }
    }

    readonly record struct WindowMaxRect(int MaxPositionX, int MaxPositionY, int MaxWidth, int MaxHeight, int maxTrackWidth, int maxTrackHeight)
    {
        public static readonly WindowMaxRect Empty = new WindowMaxRect();
    }

    enum AppBarHideMode
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }
}
