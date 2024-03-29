using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NiVE3.UI.Primitive
{
    /// <summary>
    /// ContextMenuSelectBox.xaml の相互作用ロジック
    /// </summary>
    public partial class ContextMenuSelectBox : UserControl
    {
        public static readonly DependencyProperty SelectedItemTemplateProperty = DependencyProperty.Register(
            nameof(SelectedItemTemplate),
            typeof(DataTemplate),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SelectedItemTemplateSelectorProperty = DependencyProperty.Register(
            nameof(SelectedItemTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ItemTemplateSelectorProperty = DependencyProperty.Register(
            nameof(ItemTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.Register(
            nameof(ItemContainerStyle),
            typeof(Style),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty AlternationCountProperty = DependencyProperty.Register(
            nameof(AlternationCount),
            typeof(int),
            typeof(ContextMenuSelectBox),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public int AlternationCount
        {
            get { return (int)GetValue(AlternationCountProperty); }
            set { SetValue(AlternationCountProperty, value); }
        }

        public IEnumerable? ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public object? SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public Style? ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        public DataTemplateSelector? ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public DataTemplate? ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public DataTemplateSelector? SelectedItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(SelectedItemTemplateSelectorProperty); }
            set { SetValue(SelectedItemTemplateSelectorProperty, value); }
        }

        public DataTemplate? SelectedItemTemplate
        {
            get { return (DataTemplate)GetValue(SelectedItemTemplateProperty); }
            set { SetValue(SelectedItemTemplateProperty, value); }
        }

        ContextMenu SelectorMenu => (ContextMenu)Resources[ContextMenuSelectBoxConst.SelectorMenu];

        public static readonly RoutedEvent SelectItemChangedByUserEvent = EventManager.RegisterRoutedEvent(
            nameof(SelectItemChangedByUser), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ContextMenuSelectBox)
        );

        public static readonly RoutedEvent SelectItemChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(SelectItemChanged), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ContextMenuSelectBox)
        );

        public event RoutedEventHandler SelectItemChanged
        {
            add { AddHandler(SelectItemChangedEvent, value); }
            remove { RemoveHandler(SelectItemChangedEvent, value); }
        }

        public event RoutedEventHandler SelectItemChangedByUser
        {
            add { AddHandler(SelectItemChangedByUserEvent, value); }
            remove { RemoveHandler(SelectItemChangedByUserEvent, value); }
        }

        public ContextMenuSelectBox()
        {
            InitializeComponent();
            SelectorMenu.PlacementTarget = this;
        }

        private void Root_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && (e.RightButton != MouseButtonState.Pressed || ContextMenu == null))
            {
                SelectorMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void SelectorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                var newItem = item.DataContext;
                if ((SelectedItem != newItem) || !(SelectedItem?.Equals(newItem) ?? false))
                {
                    SetCurrentValue(SelectedItemProperty, newItem);
                    RaiseEvent(new RoutedEventArgs(SelectItemChangedByUserEvent, this));
                }
            }
        }
    }

    /// <summary>
    /// セパレーター用クラス
    /// </summary>
    public class ContextMenuSelectBoxSeparator { }

    // NOTE: publicにはしたくないが、x:Staticがpublicなフィールドにしかアクセスできない(クラスがpublicかどうかは問わない)ため、別クラスに定数を切り出す
    class ContextMenuSelectBoxConst
    {
        public const string SelectorMenu = nameof(SelectorMenu);
    }
}
