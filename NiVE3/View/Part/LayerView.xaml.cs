using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using NiVE3.Plugin.Interfaces;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// LayerView.xaml の相互作用ロジック
    /// </summary>
    public partial class LayerView : UserControl
    {
        // NOTE: なぜかTypeConverterをSourceTypeにつけてもNREが出てXAML上でリソースとして定義出来ないため、定数として定義する
        public static readonly SourceType CompositionDisplayableSourceType = SourceType.Image | SourceType.Video;

        public static RoutedEvent IsDurationEditingChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(IsDurationEditingChanged), RoutingStrategy.Direct, typeof(EventHandler), typeof(LayerView)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty LayerControlAreaWidthProperty = DependencyProperty.Register(
            nameof(LayerControlAreaWidth),
            typeof(double),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty LayerNumberProperty = DependencyProperty.Register(
            nameof(LayerNumber),
            typeof(int),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0.0)
        );

        private static readonly DependencyPropertyKey IsDurationEditingPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsDurationEditing),
            typeof(bool),
            typeof(LayerView),
            new FrameworkPropertyMetadata(false, IsDurationEditingChangedHandler)
        );

        public static readonly DependencyProperty IsDurationEditingProperty = IsDurationEditingPropertyKey.DependencyProperty;

        public event EventHandler IsDurationEditingChanged
        {
            add { AddHandler(IsDurationEditingChangedEvent, value); }
            remove { RemoveHandler(IsDurationEditingChangedEvent, value); }
        }

        public bool IsDurationEditing
        {
            get { return (bool)GetValue(IsDurationEditingProperty); }
            private set { SetValue(IsDurationEditingPropertyKey, value); }
        }

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
        }

        public int LayerNumber
        {
            get { return (int)GetValue(LayerNumberProperty); }
            set { SetValue(LayerNumberProperty, value); }
        }

        public double LayerControlAreaWidth
        {
            get { return (double)GetValue(LayerControlAreaWidthProperty); }
            set { SetValue(LayerControlAreaWidthProperty, value); }
        }

        public double RangeStart
        {
            get { return (double)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public double Range
        {
            get { return (double)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        LayerCollectionView? ParentCollection => ItemsControl.ItemsControlFromItemContainer(this) as LayerCollectionView;

        LayerViewModel? ViewModel => DataContext as LayerViewModel;

        public LayerView()
        {
            InitializeComponent();
        }

        static bool IsClickSameControl(FrameworkElement fe, MouseButtonEventArgs e)
        {
            return new Rect(0.0, 0.0, fe.ActualWidth, fe.ActualHeight).Contains(e.GetPosition(fe));
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            ParentCollection?.SelectItem(this, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) ||  Keyboard.IsKeyDown(Key.RightCtrl));
        }

        private void LayerNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            if ((e.Key == Key.Tab || (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)) && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                LayerNameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && viewModel.EndEditNameCommand.CanExecute(false))
            {
                viewModel.EndEditNameCommand.Execute(false);
                LayerNameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void LayerNameTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (!IsClickSameControl(LayerNameTextBox, e) && viewModel != null && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                LayerNameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void LayerCommentGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || e.ClickCount != 2)
            {
                return;
            }

            var viewModel = ViewModel;
            if (viewModel != null && viewModel.BeginEditCommentCommand.CanExecute(null))
            {
                viewModel.BeginEditCommentCommand.Execute(null);
                LayerCommentTextBox.Focus();
                LayerCommentTextBox.SelectAll();
                LayerCommentTextBox.CaptureMouse();
                e.Handled = true;
            }
        }

        private void LayerCommentTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            if ((e.Key == Key.Tab || (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)) && viewModel.EndEditCommentCommand.CanExecute(true))
            {
                viewModel.EndEditCommentCommand.Execute(true);
                LayerCommentTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && viewModel.EndEditCommentCommand.CanExecute(false))
            {
                viewModel.EndEditCommentCommand.Execute(false);
                LayerCommentTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void LayerCommentTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (!IsClickSameControl(LayerCommentTextBox, e) && viewModel != null && viewModel.EndEditCommentCommand.CanExecute(true))
            {
                viewModel.EndEditCommentCommand.Execute(true);
                LayerCommentTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void LayerNameTextBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // NOTE: なぜかCaptureMouse後にマウスキャプチャが外れるため再度キャプチャする
            // TODO: キャプチャが外れる原因の調査
            if (ViewModel?.EditingParameter == EditingLayerParameter.Name)
            {
                LayerNameTextBox.CaptureMouse();
            }
        }

        private void LayerCommentTextBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // NOTE: なぜかCaptureMouse後にマウスキャプチャが外れるため再度キャプチャする
            // TODO: キャプチャが外れる原因の調査
            if (ViewModel?.EditingParameter == EditingLayerParameter.Comment)
            {
                LayerCommentTextBox.CaptureMouse();
            }
        }

        private void LayerNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel?.EditingParameter == EditingLayerParameter.Name)
            {
                LayerNameTextBox.Focus();
                LayerNameTextBox.SelectAll();
                LayerNameTextBox.CaptureMouse();
            }
        }

        private void BlendModeComboBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeBlendModeCommand?.Execute(BlendModeComboBox.SelectedItem);
        }

        private void DurationBar_IsClickedChanged(object sender, EventArgs e)
        {
            IsDurationEditing = DurationBar.IsClicked;
            if (IsDurationEditing)
            {
                ViewModel?.BeginEditDurationCommand?.Execute(null);
            }
            else
            {
                ViewModel?.CommitEditDurationCommand?.Execute(null);
            }
        }

        static void IsDurationEditingChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerView layer)
            {
                layer.RaiseEvent(new RoutedEventArgs(IsDurationEditingChangedEvent, d));
            }
        }
    }
}
