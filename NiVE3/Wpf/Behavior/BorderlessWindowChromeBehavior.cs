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
            }
            return nint.Zero;
        }

        static nint CalcNonClientSize(nint hwnd, nint lParam, ref bool handled)
        {
            if (!NativeMethods.IsZoomed(hwnd))
            {
                return (nint)WVP.WVR_REDRAW;
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
            switch (MonitorDimension.CheckHasAppBarAutoHide(monitorInfo.rcMonitor))
            {
                case AppBarHideMode.Left:
                    rcWork.Left += HideAppBarSpace;
                    break;
                case AppBarHideMode.Top:
                    rcWork.Top += HideAppBarSpace;
                    break;
                case AppBarHideMode.Right:
                    rcWork.Right -= HideAppBarSpace;
                    break;
                case AppBarHideMode.Bottom:
                    rcWork.Bottom -= HideAppBarSpace;
                    break;
            }

            size.rgrc[0] = rcWork;
            size.rgrc[1] = rcWork;
            size.rgrc[2] = rcWork;
            Marshal.StructureToPtr(size, lParam, true);

            handled = true;
            return (nint)(WVP.WVR_ALIGNTOP | WVP.WVR_ALIGNLEFT | WVP.WVR_ALIGNRIGHT | WVP.WVR_ALIGNBOTTOM | WVP.WVR_VALIDRECTS);
        }
    }
}
