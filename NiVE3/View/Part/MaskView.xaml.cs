using System;
using System.Collections.Generic;
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
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Converter;
using NiVE3.View.Resource;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// MaskView.xaml の相互作用ロジック
    /// </summary>
    public partial class MaskView : UserControl
    {
        public static readonly IValueConverter NextIndentConverter = new DelegateConverter<int, int>(v => v + 1);

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(MaskView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(MaskView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(MaskView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IsCommentColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsCommentColumnVisible),
            typeof(bool),
            typeof(MaskView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(MaskView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(MaskView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(MaskView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(MaskView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(MaskView),
            new FrameworkPropertyMetadata(0.0)
        );

        private static readonly DependencyProperty CalculatedControlAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedControlAreaWidth),
            typeof(GridLength),
            typeof(MaskView),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
        }

        public Time RangeStart
        {
            get { return (Time)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public Time Range
        {
            get { return (Time)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public double NameAreaWidth
        {
            get { return (double)GetValue(NameAreaWidthProperty); }
            set { SetValue(NameAreaWidthProperty, value); }
        }

        public double ControlAreaWidth
        {
            get { return (double)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        public bool IsCommentColumnVisible
        {
            get { return (bool)GetValue(IsCommentColumnVisibleProperty); }
            set { SetValue(IsCommentColumnVisibleProperty, value); }
        }

        public bool IsTagColumnVisible
        {
            get { return (bool)GetValue(IsTagColumnVisibleProperty); }
            set { SetValue(IsTagColumnVisibleProperty, value); }
        }

        public bool IsAVSwitchColumnVisible
        {
            get { return (bool)GetValue(IsAVSwitchColumnVisibleProperty); }
            set { SetValue(IsAVSwitchColumnVisibleProperty, value); }
        }

        public int IndentLevel
        {
            get { return (int)GetValue(IndentLevelProperty); }
            set { SetValue(IndentLevelProperty, value); }
        }

        public GridLength CalculatedControlAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedControlAreaWidthProperty); }
            set { SetValue(CalculatedControlAreaWidthProperty, value); }
        }

        MaskViewModel? ViewModel => DataContext as MaskViewModel;

        MaskCollectionView? ParentCollection => ItemsControl.ItemsControlFromItemContainer(this) as MaskCollectionView;

        public MaskView()
        {
            InitializeComponent();
        }

        private void Root_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
            {
                return;
            }

            Focus();
            ParentCollection?.SelectItem(this, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            ViewModel?.SelectItemCommand?.Execute(null);
            e.Handled = true;
        }

        static void ControlAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MaskView maskView)
            {
                var indent = UIParameters.ArrowWidth * maskView.IndentLevel;
                if (maskView.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                var nameAreaWidth = maskView.NameAreaWidth - UIParameters.ArrowWidth * maskView.IndentLevel + UIParameters.ArrowWidth;
                if (maskView.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                maskView.CalculatedControlAreaWidth = new GridLength(Math.Max(maskView.ControlAreaWidth - indent - nameAreaWidth, 0.0));
            }
        }
    }
}
