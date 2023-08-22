using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Util;

namespace NiVE3.Wpf.Behavior
{
    class BorderlessWindowChromeBehavior : WndProcBehavior<Window>
    {
        // SEE: https://mntone.hateblo.jp/entry/2020/08/02/111309

        const int HideAppBarSpace = 2;

        protected override nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam, ref bool handled)
        {
            switch (msg)
            {
                case (uint)WM.WM_NCCALCSIZE:
                    if (wParam != nint.Zero)
                    {
                        var result = CalcNonClientSize(hwnd, lParam, ref handled);
                        if (handled)
                        {
                            return result;
                        }
                    }
                    break;
                case (uint)WM.WM_GETMINMAXINFO:
                    CalcMinMax(hwnd, lParam);
                    handled = true;
                    break;
            }
            return nint.Zero;
        }

        static nint CalcNonClientSize(nint hwnd, nint lParam, ref bool handled)
        {
            if (!NativeMethods.IsZoomed(hwnd))
            {
                return nint.Zero;
            }

            var size = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
            if (size.lppos.flags.HasFlag(SWP.SWP_NOSIZE))
            {
                return nint.Zero;
            }

            var hMonitor = NativeMethods.MonitorFromWindow(hwnd, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == nint.Zero)
            {
                return nint.Zero;
            }

            var monitorInfo = new MONITORINFO();
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                return nint.Zero;
            }

            var rcWork = monitorInfo.rcWork;
            switch (CheckHasAppBarAutoHide(monitorInfo.rcMonitor))
            {
                case (true, _, _, _):
                    rcWork.Left += HideAppBarSpace;
                    break;
                case (_, true, _, _):
                    rcWork.Top += HideAppBarSpace;
                    break;
                case (_, _, true, _):
                    rcWork.Right -= HideAppBarSpace;
                    break;
                case (_, _, _, true):
                    rcWork.Bottom -= HideAppBarSpace;
                    break;
            }

            size.rgrc[0] = rcWork;
            size.rgrc[1] = rcWork;
            size.rgrc[2] = rcWork;
            Marshal.StructureToPtr(size, lParam, true);

            handled = true;
            return (nint)(WVP.WVR_ALIGNTOP | WVP.WVR_ALIGNLEFT | WVP.WVR_VALIDRECTS);
        }

        static void CalcMinMax(nint hwnd, nint lParam)
        {
            var hMonitor = NativeMethods.MonitorFromWindow(hwnd, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new MONITORINFO();
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                return;
            }

            var rcWork = monitorInfo.rcWork;
            var rcMonitor = monitorInfo.rcMonitor;
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            mmi.ptMaxPosition.x = Math.Abs(rcWork.Left - rcMonitor.Left);
            mmi.ptMaxPosition.y = Math.Abs(rcWork.Top - rcMonitor.Top);
            mmi.ptMaxSize.x = Math.Abs(rcWork.Right - rcWork.Left) + 2;
            mmi.ptMaxSize.y = Math.Abs(rcWork.Bottom - rcWork.Top) + 2;
            mmi.ptMaxTrackSize = mmi.ptMaxSize;
            mmi.ptMinTrackSize = new POINT { x = 800, y = 600 }; // TODO: 最小サイズの調整

            switch (CheckHasAppBarAutoHide(rcMonitor))
            {
                case (true, _, _, _):
                    mmi.ptMaxPosition.x += HideAppBarSpace;
                    mmi.ptMaxTrackSize.x -= HideAppBarSpace;
                    mmi.ptMaxSize.x -= HideAppBarSpace;
                    break;
                case (_, true, _, _):
                    mmi.ptMaxPosition.y += HideAppBarSpace;
                    mmi.ptMaxTrackSize.y -= HideAppBarSpace;
                    mmi.ptMaxSize.y -= HideAppBarSpace;
                    break;
                case (_, _, true, _):
                    mmi.ptMaxTrackSize.x -= HideAppBarSpace;
                    mmi.ptMaxSize.x -= HideAppBarSpace;
                    break;
                case (_, _, _, true):
                    mmi.ptMaxTrackSize.y -= HideAppBarSpace;
                    mmi.ptMaxSize.y -= HideAppBarSpace;
                    break;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        static (bool left, bool top, bool right, bool bottom) CheckHasAppBarAutoHide(RECT rc)
        {
            return (
                HasAppBarAutoHide(rc, ABE.ABE_LEFT),
                HasAppBarAutoHide(rc, ABE.ABE_TOP),
                HasAppBarAutoHide(rc, ABE.ABE_RIGHT),
                HasAppBarAutoHide(rc, ABE.ABE_BOTTOM)
            );
        }

        static bool HasAppBarAutoHide(RECT rc, ABE edge)
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
}
