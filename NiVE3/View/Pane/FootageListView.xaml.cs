using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            typeof(ObservableCollection<TreeListViewItem>),
            typeof(FootageListView),
            new FrameworkPropertyMetadata(new ObservableCollection<TreeListViewItem>(), SelectedItemChanged)
        );

        public ObservableCollection<TreeListViewItem> SelectedItems
        {
            get { return (ObservableCollection<TreeListViewItem>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public FootageListView()
        {
            InitializeComponent();
            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
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
            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)
            {
                ViewModel?.EndEditPropertyCommand?.Execute(null);
            }
        }

        private void SelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ViewModel?.EndEditPropertyCommand?.Execute(null);
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
    }
}
