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

        public static T? FindVisualChild<T>(this DependencyObject parent, bool traverse = false) where T : DependencyObject
        {
            return parent.FindVisualChild<T>(_ => true, traverse);
        }

        public static T? FindVisualChild<T>(this DependencyObject parent, Predicate<T> predicate, bool traverse = false) where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            var children = new DependencyObject[childCount];
            for (var i = 0; i < childCount; i++)
            {
                var c = VisualTreeHelper.GetChild(parent, i);
                if (c is T child && predicate(child))
                {
                    return child;
                }

                children[i] = c;
            }

            if (traverse)
            {
                foreach (var c in children)
                {
                    var t = c.FindVisualChild(predicate);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }

            return null;
        }
    }
}
