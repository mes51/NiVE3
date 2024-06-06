using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
using System.Windows.Threading;
using NiVE3.Extension;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// MultiItemRadioButton.xaml の相互作用ロジック
    /// </summary>
    [DefaultProperty(nameof(Items))]
    [ContentProperty(nameof(Items))]
    public partial class MultiItemRadioButton : RadioButton
    {
        readonly TimeSpan LongClickTimerInterval = new TimeSpan(0, 0, 0, 0, 300);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MultiItemRadioButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, ItemsSourcePropertyChanged)
        );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(MultiItemRadioButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedItemPropertyChanged)
        );

        public static readonly DependencyProperty ActiveItemProperty = DependencyProperty.Register(
            nameof(ActiveItem),
            typeof(object),
            typeof(MultiItemRadioButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, ActiveItemPropertyChanged)
        );

        public static readonly DependencyProperty ActiveItemIndexProperty = DependencyProperty.Register(
            nameof(ActiveItemIndex),
            typeof(int),
            typeof(MultiItemRadioButton),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, ActiveItemIndexPropertyChanged)
        );

        public static readonly DependencyProperty ItemSelectMenuItemTemplateProperty = DependencyProperty.Register(
            nameof(ItemSelectMenuItemTemplate),
            typeof(DataTemplate),
            typeof(MultiItemRadioButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, ItemSelectMenuItemTemplatePropertyChanged)
        );

        public IEnumerable? ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public object? SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public object? ActiveItem
        {
            get { return GetValue(ActiveItemProperty); }
            set { SetValue(ActiveItemProperty, value); }
        }

        public int ActiveItemIndex
        {
            get { return (int)GetValue(ActiveItemIndexProperty); }
            set { SetValue(ActiveItemIndexProperty, value); }
        }

        public DataTemplate? ItemSelectMenuItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemSelectMenuItemTemplateProperty); }
            set { SetValue(ItemSelectMenuItemTemplateProperty, value); }
        }

        public ObservableCollection<object> Items { get; set; } = [];

        DispatcherTimer LongClickTimer { get; } = new DispatcherTimer();

        public MultiItemRadioButton()
        {
            LongClickTimer.Interval = LongClickTimerInterval;
            LongClickTimer.Tick += (sender, e) =>
            {
                LongClickTimer.Stop();
                ItemSelectMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                ItemSelectMenu.PlacementTarget = this;
                ItemSelectMenu.IsOpen = true;
            };
            Items.CollectionChanged += ItemsCollection_CollectionChanged;

            InitializeComponent();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            LongClickTimer.Start();
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            LongClickTimer.Stop();
            base.OnPreviewMouseLeftButtonUp(e);
        }

        private void Root_Checked(object? sender, RoutedEventArgs e)
        {
            if (ActiveItemIndex > -1)
            {
                SelectedItem = ActiveItem;
            }
        }

        private void ItemsCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ItemsSource = Items;
        }

        private void ItemSelectMenuMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                ActiveItem = menuItem.Tag;
                SelectedItem = menuItem.Tag;
            }
        }

        private static void ItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not MultiItemRadioButton radioButton)
            {
                return;
            }

            radioButton.ItemSelectMenu.ItemsSource = radioButton.ItemsSource;
            if (radioButton.ItemsSource != null)
            {
                radioButton.ActiveItemIndex = (radioButton.ItemsSource?.Cast<object>() ?? []).IndexOf(radioButton.ActiveItem);
            }
            else
            {
                radioButton.ActiveItemIndex = -1;
                radioButton.IsChecked = false;
            }
        }

        private static void SelectedItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not MultiItemRadioButton radioButton)
            {
                return;
            }

            if ((radioButton.ItemsSource?.Cast<object>() ?? []).Contains(radioButton.SelectedItem))
            {
                radioButton.ActiveItem = radioButton.SelectedItem;
            }
            radioButton.IsChecked = Equals(radioButton.SelectedItem, radioButton.ActiveItem);
        }

        private static void ActiveItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not MultiItemRadioButton radioButton)
            {
                return;
            }

            radioButton.ActiveItemIndex = radioButton.ItemsSource?.Cast<object>()?.IndexOf(radioButton.ActiveItem) ?? -1;
            if (radioButton.ActiveItemIndex < 0)
            {
                radioButton.ActiveItem = null;
            }
        }

        private static void ActiveItemIndexPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not MultiItemRadioButton radioButton || e.NewValue == e.OldValue)
            {
                return;
            }

            if (radioButton.ActiveItemIndex > -1 && radioButton.ActiveItemIndex < (radioButton.ItemsSource?.Cast<object>()?.Count() ?? 0))
            {
                radioButton.ActiveItem = (radioButton.ItemsSource?.Cast<object>() ?? []).ElementAt(radioButton.ActiveItemIndex);
                radioButton.IsChecked = Equals(radioButton.SelectedItem, radioButton.ActiveItem);
            }
            else
            {
                radioButton.ActiveItem = null;
                radioButton.IsChecked = false;
            }
        }

        private static void ItemSelectMenuItemTemplatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is MultiItemRadioButton radioButton)
            {
                radioButton.ItemSelectMenu.ItemTemplate = radioButton.ItemSelectMenuItemTemplate;
            }
        }
    }
}
