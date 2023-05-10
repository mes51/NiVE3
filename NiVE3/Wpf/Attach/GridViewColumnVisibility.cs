using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NiVE3.Wpf.Attach
{
    class GridViewColumnVisibility
    {
        const double DefaultWidth = 100.0;

        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.RegisterAttached(
            "Visibility",
            typeof(bool),
            typeof(GridViewColumnVisibility), new PropertyMetadata(false, VisibilityChanged)
        );

        static Dictionary<WeakReference<GridViewColumn>, double> Widths { get; } = new Dictionary<WeakReference<GridViewColumn>, double>(new WeakReferenceEqualityComparer<GridViewColumn>());

        public static bool GetVisibility(DependencyObject obj)
        {
            return (bool)obj.GetValue(VisibilityProperty);
        }

        public static void SetVisibility(DependencyObject obj, bool value)
        {
            obj.SetValue(VisibilityProperty, value);
        }

        static void VisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is GridViewColumn column)
            {
                if (e.NewValue is bool visible)
                {
                    if (visible)
                    {
                        if (Widths.TryGetValue(new WeakReference<GridViewColumn>(column), out var width))
                        {
                            column.Width = width;
                        }
                        else
                        {
                            column.Width = column.Width <= 0.0 ? DefaultWidth : column.Width;
                        }
                    }
                    else
                    {
                        var key = new WeakReference<GridViewColumn>(column);
                        if (Widths.ContainsKey(key))
                        {
                            Widths[key] = column.Width;
                        }
                        else
                        {
                            Widths.Add(key, column.Width);
                        }

                        column.Width = 0.0;
                    }
                }
            }
        }
    }

    class WeakReferenceEqualityComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
    {
        public bool Equals(WeakReference<T>? x, WeakReference<T>? y)
        {
            if ((x?.TryGetTarget(out var vx) ?? false) && (y?.TryGetTarget(out var vy) ?? false))
            {
                return EqualityComparer<T>.Default.Equals(vx, vy);
            }
            else
            {
                return false;
            }
        }

        public int GetHashCode([DisallowNull] WeakReference<T> obj)
        {
            if (obj.TryGetTarget(out var v))
            {
                return v.GetHashCode();
            }
            else
            {
                return 0;
            }
        }
    }
}
