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
using NiVE3.Extension;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Converter;
using NiVE3.View.Resource;
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

        public static readonly IMultiValueConverter CycledParentLayerConverter = new DelegateMultiValueConverter<LayerViewModel, Guid?, bool>((vm, l) => vm.CheckParentLayerCycled(l));

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

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            ParentCollection?.SelectItem(this, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            ViewModel?.SelectItemCommand?.Execute(null);
            e.Handled = true;
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
                e.Handled = true;
            }
        }

        private void BlendModeSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeBlendModeCommand?.Execute(BlendModeSelectBox.SelectedItem);
        }

        private void TrackMatteSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeTrackMatteCommand?.Execute(TrackMatteSelectBox.SelectedItem);
        }

        private void TrackMatteModeSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeTrackMatteModeCommand?.Execute(TrackMatteModeSelectBox.SelectedItem);
        }

        private void ParentLayerSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeParentLayerCommand?.Execute(ParentLayerSelectBox.SelectedItem);
        }

        private void DurationBar_IsClickedChanged(object sender, EventArgs e)
        {
            if (DurationBar.IsClicked)
            {
                ViewModel?.BeginEditDurationCommand?.Execute(null);
            }
            else
            {
                ViewModel?.CommitEditDurationCommand?.Execute(null);
            }
        }
    }
}
