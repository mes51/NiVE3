using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
using System.Windows.Threading;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// FootageView.xaml の相互作用ロジック
    /// </summary>
    public partial class FootageListView : UserControl
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(ObservableCollection<object>),
            typeof(FootageListView),
            new FrameworkPropertyMetadata(new ObservableCollection<object>(), SelectedItemChanged)
        );

        private static readonly DependencyPropertyKey SelectedFootagesPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectedFootages),
            typeof(ObservableCollection<IFootageViewModel>),
            typeof(FootageListView),
            new FrameworkPropertyMetadata(new ObservableCollection<IFootageViewModel>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        internal static readonly DependencyProperty SelectedFootagesProperty = SelectedFootagesPropertyKey.DependencyProperty;

        public ObservableCollection<object> SelectedItems
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        internal ObservableCollection<IFootageViewModel> SelectedFootages
        {
            get { return (ObservableCollection<IFootageViewModel>)GetValue(SelectedFootagesProperty); }
            private set { SetValue(SelectedFootagesPropertyKey, value); }
        }

        public FootageListView()
        {
            InitializeComponent();

            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(GridViewColumnHeader_Clicked));
        }

        FootageListViewModel? ViewModel => DataContext as FootageListViewModel;

        private void TreeListViewItem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is TreeListViewItem targetItem)
            {
                if (sender is FrameworkElement fe && fe.DataContext is IFootageViewModel vm)
                {
                    ViewModel?.BeginEditPropertyCommand?.Execute(Tuple.Create(vm, nameof(IFootageViewModel.Name)));
                }
            }
        }

        private void FootageCommentDisplayText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                if (sender is FrameworkElement fe && fe.DataContext is IFootageViewModel vm)
                {
                    ViewModel?.BeginEditPropertyCommand?.Execute(Tuple.Create(vm, nameof(IFootageViewModel.Comment)));
                }
            }
        }

        private void FootageEditTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: Escによるキャンセル
            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)
            {
                ViewModel?.EndEditPropertyCommand?.Execute(null);
            }
        }

        private void SelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ViewModel?.EndEditPropertyCommand?.Execute(null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    SelectedFootages.Clear();
                    break;
                case NotifyCollectionChangedAction.Add when e.NewItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel newItem:
                    SelectedFootages.Add(newItem);
                    break;
                case NotifyCollectionChangedAction.Remove when e.OldItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel oldItem:
                    SelectedFootages.Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Replace when e.NewItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel newItem:
                    SelectedFootages[e.NewStartingIndex] = newItem;
                    break;
            }
        }

        static void SelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FootageListView footageListView)
            {
                if (e.OldValue is ObservableCollection<TreeViewItem> oldItems)
                {
                    oldItems.CollectionChanged += footageListView.SelectedItems_CollectionChanged;
                }
                if (e.NewValue is ObservableCollection<TreeViewItem> newItems)
                {
                    newItems.CollectionChanged += footageListView.SelectedItems_CollectionChanged;
                }
            }
        }

        private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox editTextBox)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    editTextBox.CaretIndex = int.MaxValue;
                    editTextBox.Focus();
                }, DispatcherPriority.Render);
            }
        }

        private void GridViewColumnHeader_Clicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
