using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace NiVE3.Extension
{
    static class DependencyObjectExtensions
    {
        public static T? FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            return child.FindVisualParent<T>(_ => true);
        }

        public static T? FindVisualParent<T>(this DependencyObject child, Predicate<T> predicate) where T : DependencyObject
        {
            var p = VisualTreeHelper.GetParent(child);
            if (p is T parent && predicate(parent))
            {
                return parent;
            }
            else
            {
                return p?.FindVisualParent(predicate);
            }
        }

        public static T? FindLogicalParent<T>(this DependencyObject child) where T : DependencyObject
        {
            return child.FindLogicalParent<T>(_ => true);
        }

        public static T? FindLogicalParent<T>(this DependencyObject child, Predicate<T> predicate) where T : DependencyObject
        {
            var p = LogicalTreeHelper.GetParent(child);
            if (p is T parent && predicate(parent))
            {
                return parent;
            }
            else
            {
                return p?.FindLogicalParent(predicate);
            }
        }
    }
}
