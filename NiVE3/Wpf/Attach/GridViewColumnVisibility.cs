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
            typeof(GridViewColumnVisibility),
            new PropertyMetadata(false, VisibilityChanged)
        );

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
            "Columns",
            typeof(GridViewColumnCollection),
            typeof(GridViewColumnVisibility),
            new PropertyMetadata(new GridViewColumnCollection(), ColumnsChanged)
        );

        private static readonly DependencyProperty PrivateRegisteredColumnsProperty = DependencyProperty.RegisterAttached(
            "PrivateRegisteredColumns",
            typeof(GridViewColumn[]),
            typeof(GridViewColumnVisibility),
            new PropertyMetadata(Array.Empty<GridViewColumn>())
        );

        private static readonly DependencyProperty PrivateColumnParentProperty = DependencyProperty.RegisterAttached(
            "PrivateColumnParent",
            typeof(DependencyObject),
            typeof(GridViewColumnVisibility),
            new PropertyMetadata(null)
        );

        public static GridViewColumnCollection GetColumns(DependencyObject obj)
        {
            return (GridViewColumnCollection)obj.GetValue(ColumnsProperty);
        }

        public static void SetColumns(DependencyObject obj, GridViewColumnCollection value)
        {
            obj.SetValue(ColumnsProperty, value);
        }

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
            if (obj is GridViewColumn column && column.GetValue(PrivateColumnParentProperty) is DependencyObject parent && parent.GetValue(PrivateRegisteredColumnsProperty) is GridViewColumn[] registeredColumns && registeredColumns.Length > 0)
            {
                var columns = GetColumns(parent);
                if (e.NewValue is bool visible)
                {
                    if (visible)
                    {
                        if (columns.Contains(column))
                        {
                            return;
                        }

                        var index = Array.IndexOf(registeredColumns, column);
                        if (index < 0)
                        {
                            return;
                        }

                        for (var i = 0; i < columns.Count; i++)
                        {
                            var ci = Array.IndexOf(registeredColumns, columns[i]);
                            if (ci < 0)
                            {
                                continue;
                            }

                            if (index < ci)
                            {
                                columns.Insert(i, column);
                                return;
                            }
                        }

                        columns.Add(column);
                    }
                    else
                    {
                        if (!columns.Contains(column))
                        {
                            return;
                        }

                        columns.Remove(column);
                    }
                }
            }
        }

        static void ColumnsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GridViewColumnCollection collection)
            {
                obj.SetValue(PrivateRegisteredColumnsProperty, collection.ToArray());
                foreach (var column in collection)
                {
                    column.SetValue(PrivateColumnParentProperty, obj);
                }
            }
            else
            {
                obj.SetValue(PrivateRegisteredColumnsProperty, Array.Empty<GridViewColumn>());
            }
        }
    }
}
