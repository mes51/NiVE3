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

        FootageListViewModel? ViewModel => DataContext as FootageListViewModel;

        public FootageListView()
        {
            InitializeComponent();

            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(GridViewColumnHeader_Clicked));
        }

        static bool IsClickSameControl(FrameworkElement fe, MouseButtonEventArgs e)
        {
            return new Rect(0.0, 0.0, fe.ActualWidth, fe.ActualHeight).Contains(e.GetPosition(fe));
        }

        private void FootageCommentDisplayText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                var viewModel = ViewModel;
                if (viewModel != null && sender is FrameworkElement fe && fe.DataContext is IFootageViewModel vm && viewModel.BeginEditCommentCommand.CanExecute(vm))
                {
                    viewModel.BeginEditCommentCommand.Execute(vm);
                }
            }
        }

        private void TreeListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is FootageViewModel vm)
            {
                ViewModel?.ShowPreviewCommand?.Execute(vm);
            }
        }

        private void FootageNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || sender is not TextBox textBox)
            {
                return;
            }

            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                textBox.ReleaseMouseCapture();
            }
            else if (e.Key == Key.Escape && viewModel.EndEditNameCommand.CanExecute(false))
            {
                viewModel.EndEditNameCommand.Execute(false);
                textBox.ReleaseMouseCapture();
            }
        }

        private void FootageNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel?.EditingParameter == EditingFootageParameter.Name && sender is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
                textBox.CaptureMouse();
            }
        }

        private void FootageCommentTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || sender is not TextBox textBox)
            {
                return;
            }

            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None && viewModel.EndEditCommentCommand.CanExecute(true))
            {
                viewModel.EndEditCommentCommand.Execute(true);
                textBox.ReleaseMouseCapture();
            }
            else if (e.Key == Key.Escape && viewModel.EndEditCommentCommand.CanExecute(false))
            {
                viewModel.EndEditCommentCommand.Execute(false);
                textBox.ReleaseMouseCapture();
            }
        }

        private void FootageCommentTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel?.EditingParameter == EditingFootageParameter.Comment && sender is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
                textBox.CaptureMouse();
            }
        }

        private void FootageNameTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || sender is not TextBox textBox)
            {
                return;
            }

            if (!IsClickSameControl(textBox, e) && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                textBox.ReleaseMouseCapture();
            }
        }

        private void FootageNameTextBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // NOTE: なぜかCaptureMouse後にマウスキャプチャが外れるため再度キャプチャする
            // TODO: キャプチャが外れる原因の調査
            if (ViewModel?.EditingParameter == EditingFootageParameter.Name && sender is TextBox textBox)
            {
                textBox.CaptureMouse();
            }
        }

        private void FootageCommentTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || sender is not TextBox textBox)
            {
                return;
            }

            if (!IsClickSameControl(textBox, e) && viewModel.EndEditCommentCommand.CanExecute(true))
            {
                viewModel.EndEditCommentCommand.Execute(true);
                textBox.ReleaseMouseCapture();
            }
        }

        private void FootageCommentTextBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // NOTE: なぜかCaptureMouse後にマウスキャプチャが外れるため再度キャプチャする
            // TODO: キャプチャが外れる原因の調査
            if (ViewModel?.EditingParameter == EditingFootageParameter.Comment && sender is TextBox textBox)
            {
                textBox.CaptureMouse();
            }
        }

        private void SelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // NOTE: ReadOnlyなDependencyPropertyはBinding出来ないためここで直接ViewModelに反映する
            // TODO: なんか良い方法探す
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    SelectedFootages.Clear();
                    ViewModel?.SelectedFootages?.Clear();
                    break;
                case NotifyCollectionChangedAction.Add when e.NewItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel newItem:
                    SelectedFootages.Add(newItem);
                    ViewModel?.SelectedFootages?.Add(newItem);
                    break;
                case NotifyCollectionChangedAction.Remove when e.OldItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel oldItem:
                    SelectedFootages.Remove(oldItem);
                    ViewModel?.SelectedFootages?.Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Replace when e.NewItems?.OfType<IFootageViewModel>()?.FirstOrDefault() is IFootageViewModel newItem:
                    SelectedFootages[e.NewStartingIndex] = newItem;
                    if (ViewModel is FootageListViewModel footageListViewModel)
                    {
                        footageListViewModel.SelectedFootages[e.NewStartingIndex] = newItem;
                    }
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

        private void GridViewColumnHeader_Clicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
