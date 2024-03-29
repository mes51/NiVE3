using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Shared.Extension;
using NiVE3.View.Converter;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// ItemResizableStackPanel.xaml の相互作用ロジック
    /// </summary>
    [ContentProperty(nameof(Children))]
    public partial class ItemResizableStackPanel : UserControl
    {
        public const double SplitterSize = 3.0;

        public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.RegisterAttached(
            "ItemSize",
            typeof(double),
            typeof(ItemResizableStackPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty ItemMinSizeProperty = DependencyProperty.RegisterAttached(
            "ItemMinSize",
            typeof(double),
            typeof(ItemResizableStackPanel),
            new PropertyMetadata(0.0)
        );

        public static readonly DependencyProperty IsLockedProperty = DependencyProperty.RegisterAttached(
            "IsLocked",
            typeof(bool),
            typeof(ItemResizableStackPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static bool GetIsLocked(UIElement target)
        {
            return (bool)target.GetValue(IsLockedProperty);
        }

        public static void SetIsLocked(UIElement target, bool value)
        {
            target.SetValue(IsLockedProperty, value);
        }

        public static double GetItemMinSize(DependencyObject target)
        {
            return (double)target.GetValue(ItemMinSizeProperty);
        }

        public static void SetItemMinSize(DependencyObject target, double value)
        {
            target.SetValue(ItemMinSizeProperty, value);
        }

        public static double GetItemSize(DependencyObject target)
        {
            return (double)target.GetValue(ItemSizeProperty);
        }

        public static void SetItemSize(DependencyObject target, double value)
        {
            target.SetValue(ItemSizeProperty, value);
        }

        static readonly OffsetGridLengthConverter SizeConverter = new OffsetGridLengthConverter { SizeOffset = -SplitterSize };

        static readonly BooleanInvertConverter IsLockedConverter = new BooleanInvertConverter();

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ItemResizableStackPanel),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OrientationChanged)
        );

        public static readonly DependencyProperty ItemCollapseByHiddenProperty = DependencyProperty.Register(
            nameof(ItemCollapseByHidden),
            typeof(bool),
            typeof(ItemResizableStackPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ItemCollapseByHiddenPropertyChanged)
        );

        public bool ItemCollapseByHidden
        {
            get { return (bool)GetValue(ItemCollapseByHiddenProperty); }
            set { SetValue(ItemCollapseByHiddenProperty, value); }
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public ObservableCollection<UIElement> Children { get; }

        // NOTE: 末尾に何かDefinitionを入れておかないと最後の要素のリサイズが出来なくなるため、それ用にDefinitionを用意しておく
        ColumnDefinition ColumnStopper { get; }

        RowDefinition RowStopper { get; }

        VisibilityToSwitchValueConverter ItemVisibilityConverter { get; } = new VisibilityToSwitchValueConverter
        {
            VisibleValue = double.PositiveInfinity,
            HiddenValue = double.PositiveInfinity,
            CollapsedValue = 0.0
        };

        VisibilityToSwitchValueConverter SplitterWidthConverter { get; } = new VisibilityToSwitchValueConverter
        {
            VisibleValue = new GridLength(SplitterSize),
            HiddenValue = new GridLength(SplitterSize),
            CollapsedValue = new GridLength()
        };

        IMultiValueConverter MinSizeConverter { get; } = new MinSizeConverter { Offset = -SplitterSize };

        public ItemResizableStackPanel()
        {
            InitializeComponent();

            ColumnStopper = new ColumnDefinition();
            RowStopper = new RowDefinition();
            Children = [];
            Children.CollectionChanged += Children_CollectionChanged;
        }

        void InsertChild(UIElement newChild, int childIndex)
        {
            var index = childIndex * 2;
            var splitter = new GridSplitter { Style = Resources["SplitterStyle"] as Style };
            BindSplitterLocked(newChild, splitter);
            ContainerGrid.Children.Insert(index, newChild);
            ContainerGrid.Children.Insert(index + 1, splitter);

            if (Orientation == Orientation.Horizontal)
            {
                if (ContainerGrid.ColumnDefinitions.Count < 1)
                {
                    ContainerGrid.ColumnDefinitions.Add(ColumnStopper);
                }

                var itemColumn = new ColumnDefinition();
                var splitterColumn = new ColumnDefinition { Width = new GridLength(SplitterSize) };
                BindColumnWidth(newChild, itemColumn);
                BindColumnSplitterWidth(newChild, splitterColumn);

                ContainerGrid.ColumnDefinitions.Insert(index, itemColumn);
                ContainerGrid.ColumnDefinitions.Insert(index + 1, splitterColumn);
            }
            else
            {
                if (ContainerGrid.RowDefinitions.Count < 1)
                {
                    ContainerGrid.RowDefinitions.Add(RowStopper);
                }

                var itemRow = new RowDefinition();
                var splitterRow = new RowDefinition { Height = new GridLength(SplitterSize) };
                BindRowHeight(newChild, itemRow);
                BindRowSplitterHeight(newChild, splitterRow);

                ContainerGrid.RowDefinitions.Insert(index, itemRow);
                ContainerGrid.RowDefinitions.Insert(index + 1, splitterRow);
            }

            for (var i = index; i < ContainerGrid.Children.Count; i++)
            {
                Grid.SetColumn(ContainerGrid.Children[i], i);
                Grid.SetRow(ContainerGrid.Children[i], i);
            }
        }

        void RemoveChild(UIElement child)
        {
            var index = ContainerGrid.Children.IndexOf(child);
            ContainerGrid.Children.RemoveAt(index + 1);
            ContainerGrid.Children.RemoveAt(index);

            if (Orientation != Orientation.Horizontal)
            {
                ContainerGrid.ColumnDefinitions.RemoveAt(index + 1);
                ContainerGrid.ColumnDefinitions.RemoveAt(index);
            }
            else
            {
                ContainerGrid.RowDefinitions.RemoveAt(index + 1);
                ContainerGrid.RowDefinitions.RemoveAt(index);
            }

            for (var i = index; i < ContainerGrid.Children.Count; i++)
            {
                Grid.SetColumn(ContainerGrid.Children[i], i);
                Grid.SetRow(ContainerGrid.Children[i], i);
            }
        }

        void ReplaceChild(UIElement oldChild, UIElement newChild)
        {
            var index = ContainerGrid.Children.IndexOf(oldChild);

            if (Orientation == Orientation.Horizontal)
            {
                BindColumnWidth(newChild, ContainerGrid.ColumnDefinitions[index]);
                BindColumnSplitterWidth(newChild, ContainerGrid.ColumnDefinitions[index + 1]);
            }
            else
            {
                BindRowHeight(newChild, ContainerGrid.RowDefinitions[index]);
                BindRowSplitterHeight(newChild, ContainerGrid.RowDefinitions[index + 1]);
            }

            ContainerGrid.Children[index] = newChild;
            BindSplitterLocked(newChild, (GridSplitter)ContainerGrid.Children[index + 1]);
            Grid.SetColumn(newChild, index);
            Grid.SetRow(newChild, index);
        }

        void MoveChild(UIElement child, int newChildIndex)
        {
            var newIndex = newChildIndex * 2;
            var oldIndex = ContainerGrid.Children.IndexOf(child);
            var splitter = ContainerGrid.Children[oldIndex + 1];

            ContainerGrid.Children.Remove(child);
            ContainerGrid.Children.Remove(splitter);
            ContainerGrid.Children.Insert(newIndex, child);
            ContainerGrid.Children.Insert(newIndex + 1, splitter);

            if (Orientation == Orientation.Horizontal)
            {
                var itemColumn = ContainerGrid.ColumnDefinitions[oldIndex];
                var splitterColumn = ContainerGrid.ColumnDefinitions[oldIndex + 1];
                ContainerGrid.ColumnDefinitions.RemoveAt(oldIndex + 1);
                ContainerGrid.ColumnDefinitions.RemoveAt(oldIndex);
                ContainerGrid.ColumnDefinitions.Insert(newIndex, itemColumn);
                ContainerGrid.ColumnDefinitions.Insert(newIndex + 1, splitterColumn);
            }
            else
            {
                var itemRow = ContainerGrid.RowDefinitions[oldIndex];
                var splitterRow = ContainerGrid.RowDefinitions[oldIndex + 1];
                ContainerGrid.RowDefinitions.RemoveAt(oldIndex + 1);
                ContainerGrid.RowDefinitions.RemoveAt(oldIndex);
                ContainerGrid.RowDefinitions.Insert(newIndex, itemRow);
                ContainerGrid.RowDefinitions.Insert(newIndex + 1, splitterRow);
            }

            for (int i = Math.Min(oldIndex, newIndex), limit = Math.Max(oldIndex, newIndex); i <= limit; i++)
            {
                Grid.SetColumn(ContainerGrid.Children[i], i);
                Grid.SetRow(ContainerGrid.Children[i], i);
            }
        }

        void UpdateOrientation()
        {
            if (Orientation == Orientation.Horizontal)
            {
                if (ContainerGrid.RowDefinitions.Count < 1)
                {
                    return;
                }
                ContainerGrid.RowDefinitions.Clear();

                for (var i = 0; i < ContainerGrid.Children.Count; i += 2)
                {
                    var child = ContainerGrid.Children[i];
                    var itemColumn = new ColumnDefinition();
                    var splitterColumn = new ColumnDefinition { Width = new GridLength(SplitterSize) };
                    BindColumnWidth(child, itemColumn);
                    BindColumnSplitterWidth(child, splitterColumn);
                    ContainerGrid.ColumnDefinitions.Add(itemColumn);
                    ContainerGrid.ColumnDefinitions.Add(splitterColumn);
                }
                ContainerGrid.ColumnDefinitions.Add(ColumnStopper);
            }
            else
            {
                if (ContainerGrid.ColumnDefinitions.Count < 1)
                {
                    return;
                }
                ContainerGrid.ColumnDefinitions.Clear();

                for (var i = 0; i < ContainerGrid.Children.Count; i++)
                {
                    var child = ContainerGrid.Children[i];
                    var itemRow = new RowDefinition();
                    var splitterRow = new RowDefinition { Height = new GridLength(SplitterSize) };
                    BindRowHeight(child, itemRow);
                    BindRowSplitterHeight(child, splitterRow);
                    ContainerGrid.RowDefinitions.Add(itemRow);
                    ContainerGrid.RowDefinitions.Add(splitterRow);
                }
                ContainerGrid.RowDefinitions.Add(RowStopper);
            }
        }

        void UpdateCollapse()
        {
            if (Orientation == Orientation.Horizontal)
            {
                foreach (var c in ContainerGrid.ColumnDefinitions)
                {
                    c.GetBindingExpression(ColumnDefinition.WidthProperty)?.UpdateTarget();
                    c.GetBindingExpression(ColumnDefinition.MinWidthProperty)?.UpdateTarget();
                    c.GetBindingExpression(ColumnDefinition.MaxWidthProperty)?.UpdateTarget();
                }
            }
            else
            {
                foreach (var c in ContainerGrid.RowDefinitions)
                {
                    c.GetBindingExpression(RowDefinition.HeightProperty)?.UpdateTarget();
                    c.GetBindingExpression(RowDefinition.MinHeightProperty)?.UpdateTarget();
                    c.GetBindingExpression(RowDefinition.MaxHeightProperty)?.UpdateTarget();
                }
            }
        }

        void BindColumnWidth(UIElement child, ColumnDefinition column)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(ItemSizeProperty),
                Source = child,
                Mode = BindingMode.TwoWay,
                Converter = SizeConverter
            };
            BindingOperations.SetBinding(column, ColumnDefinition.WidthProperty, binding);

            var minBinding = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Converter = MinSizeConverter
            };
            minBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(ItemMinSizeProperty),
                Source = child,
                Mode = BindingMode.OneWay
            });
            minBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay
            });
            BindingOperations.SetBinding(column, ColumnDefinition.MinWidthProperty, minBinding);

            var visibilityBinding = new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay,
                Converter = ItemVisibilityConverter
            };
            BindingOperations.SetBinding(column, ColumnDefinition.MaxWidthProperty, visibilityBinding);
        }

        void BindRowHeight(UIElement child, RowDefinition row)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(ItemSizeProperty),
                Source = child,
                Mode = BindingMode.TwoWay,
                Converter = SizeConverter
            };
            BindingOperations.SetBinding(row, RowDefinition.HeightProperty, binding);

            var minBinding = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Converter = MinSizeConverter
            };
            minBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(ItemMinSizeProperty),
                Source = child,
                Mode = BindingMode.OneWay
            });
            minBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay
            });
            BindingOperations.SetBinding(row, RowDefinition.MinHeightProperty, minBinding);

            var visibilityBinding = new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay,
                Converter = ItemVisibilityConverter
            };
            BindingOperations.SetBinding(row, RowDefinition.MaxHeightProperty, visibilityBinding);
        }

        void BindColumnSplitterWidth(UIElement child, ColumnDefinition column)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay,
                Converter = SplitterWidthConverter
            };
            BindingOperations.SetBinding(column, ColumnDefinition.WidthProperty, binding);
        }

        void BindRowSplitterHeight(UIElement child, RowDefinition row)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(VisibilityProperty),
                Source = child,
                Mode = BindingMode.OneWay,
                Converter = SplitterWidthConverter
            };
            BindingOperations.SetBinding(row, RowDefinition.HeightProperty, binding);
        }

        static void BindSplitterLocked(UIElement child, GridSplitter splitter)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(IsLockedProperty),
                Source = child,
                Mode = BindingMode.OneWay,
                Converter = IsLockedConverter
            };
            BindingOperations.SetBinding(splitter, IsEnabledProperty, binding);
        }

        private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var (c, i) in (e.NewItems?.Cast<UIElement>() ?? []).ZipWithIndex())
                    {
                        InsertChild(c, e.NewStartingIndex + i);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems?.Cast<UIElement>()?.FirstOrDefault() is UIElement movedChild)
                    {
                        MoveChild(movedChild, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var c in (e.OldItems?.Cast<UIElement>() ?? []))
                    {
                        RemoveChild(c);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems?.Cast<UIElement>()?.FirstOrDefault() is UIElement oldChild && e.NewItems?.Cast<UIElement>()?.FirstOrDefault() is UIElement newChild)
                    {
                        ReplaceChild(oldChild, newChild);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ContainerGrid.Children.Clear();
                    ContainerGrid.ColumnDefinitions.Clear();
                    ContainerGrid.RowDefinitions.Clear();
                    break;
            }
        }

        static void OrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ItemResizableStackPanel)?.UpdateOrientation();
        }

        static void ItemCollapseByHiddenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemResizableStackPanel panel)
            {
                if (panel.ItemCollapseByHidden)
                {
                    panel.ItemVisibilityConverter.HiddenValue = 0.0;
                    panel.SplitterWidthConverter.HiddenValue = new GridLength();
                }
                else
                {
                    panel.ItemVisibilityConverter.HiddenValue = double.PositiveInfinity;
                    panel.SplitterWidthConverter.HiddenValue = new GridLength(SplitterSize);
                }
                panel.UpdateCollapse();
            }
        }
    }

    file class MinSizeConverter : IMultiValueConverter
    {
        public double Offset { get; set; }

        public bool ItemCollapseByHidden { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1 && values[0] is double size && values[1] is Visibility visibility)
            {
                switch (visibility)
                {
                    case Visibility.Visible:
                        return Math.Max(size + Offset, 0.0);
                    case Visibility.Hidden:
                        return ItemCollapseByHidden ? 0.0 : Math.Max(size + Offset, 0.0);
                    case Visibility.Collapsed:
                        return 0.0;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
