using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using NiVE3.Extension;

namespace NiVE3.Wpf.Behavior
{
    abstract class WndProcBehavior<T> : Behavior<T>, IDisposable where T : FrameworkElement
    {
        public bool Disposed { get; private set; } = false;

        protected HwndSource? NativeWindowSource { get; set; }

        bool Initialized { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                AssociatedObject.Unloaded += AssociatedObject_Unloaded;
                if (AssociatedObject.IsLoaded)
                {
                    Initialize();
                }
                else
                {
                    AssociatedObject.Loaded += AssociatedObject_Loaded;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Dispose();
        }

        protected abstract nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam, ref bool handled);

        protected virtual void InitializeBehavior() { }

        protected virtual void UnInitializeBehavior() { }

        nint Process(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            return WndProc(hwnd, unchecked((uint)msg), wParam, lParam, ref handled);
        }

        void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            var parent = (AssociatedObject as Window) ?? AssociatedObject.FindVisualParent<Window>() ?? AssociatedObject.FindLogicalParent<Window>();
            if (parent != null)
            {
                NativeWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(parent).Handle);
                NativeWindowSource.AddHook(Process);

                InitializeBehavior();
                Initialized = true;
            }
        }

        void UnInitialize()
        {
            if (Initialized)
            {
                UnInitializeBehavior();
                NativeWindowSource?.RemoveHook(Process);
                // NOTE: 取ってきたHwndSourceはDisposeしてはダメ
                // see: https://source.dot.net/#PresentationCore/System/Windows/InterOp/HwndSource.cs,069dde8204f5aa36,references
                // see; https://stackoverflow.com/questions/8082895/how-to-use-hwndsource
                NativeWindowSource = null;
                Initialized = false;
            }
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
            if (Initialized)
            {
                AssociatedObject.Loaded -= AssociatedObject_Loaded;
            }
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Initialized)
            {
                UnInitialize();
                AssociatedObject.Loaded += AssociatedObject_Loaded;
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    if (!DesignerProperties.GetIsInDesignMode(this))
                    {
                        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
                        AssociatedObject.Loaded -= AssociatedObject_Loaded;
                        UnInitialize();
                    }
                });
                Disposed = true;

                GC.SuppressFinalize(this);
            }
        }

        ~WndProcBehavior()
        {
            Dispose();
        }
    }
}
