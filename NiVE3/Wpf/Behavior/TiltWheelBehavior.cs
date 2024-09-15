using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NiVE3.Wpf.Behavior
{
    class MouseTiltWheelEventArgs : EventArgs
    {
        public int Delta { get; }

        public MouseTiltWheelEventArgs(int delta)
        {
            Delta = delta;
        }
    }

    class TiltWheelBehavior : WndProcBehavior<FrameworkElement>
    {
        public event EventHandler<MouseTiltWheelEventArgs>? MouseTiltWheel;

        void OnMouseTiltWheel(int delta)
        {
            MouseTiltWheel?.Invoke(this, new MouseTiltWheelEventArgs(delta));
        }

        protected override nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam, ref bool handled)
        {
            const int WM_MOUSEHWHEEL = 0x020E;

            if (AssociatedObject.IsVisible)
            {
                var pos = Mouse.GetPosition(AssociatedObject);
                if (msg == WM_MOUSEHWHEEL && pos.X >= 0.0 && pos.X < AssociatedObject.ActualWidth && pos.Y >= 0.0 && pos.Y < AssociatedObject.ActualHeight)
                {
                    var delta = unchecked((int)wParam.ToInt64()) >> 16;
                    OnMouseTiltWheel(delta);
                }
            }

            return IntPtr.Zero;
        }
    }
}
