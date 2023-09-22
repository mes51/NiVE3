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
using ILGPU.IR;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.View.Resource;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// PropertyView.xaml の相互作用ロジック
    /// </summary>
    public partial class PropertyView : UserControl
    {
        const double KeyFrameSwitchWidth = 22.0;

        public static readonly GridLength KeyFrameSwitchGridLength = new GridLength(KeyFrameSwitchWidth);

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, NameAreaWidthChanged)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty CalculatedControlAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedControlAreaWidth),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        private static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ViewStateProperty = DependencyProperty.Register(
            nameof(ViewState),
            typeof(PropertyViewState),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0)
        );

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
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

        public PropertyViewState? ViewState
        {
            get { return (PropertyViewState)GetValue(ViewStateProperty); }
            set { SetValue(ViewStateProperty, value); }
        }

        public double NameAreaWidth
        {
            get { return (double)GetValue(NameAreaWidthProperty); }
            set { SetValue(NameAreaWidthProperty, value); }
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

        public double ControlAreaWidth
        {
            get { return (double)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        private GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        private GridLength CalculatedControlAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedControlAreaWidthProperty); }
            set { SetValue(CalculatedControlAreaWidthProperty, value); }
        }

        private GridLength CalculatedNameAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedNameAreaWidthProperty); }
            set { SetValue(CalculatedNameAreaWidthProperty, value); }
        }

        PropertyViewModel? ViewModel => DataContext as PropertyViewModel;

        public PropertyView()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                PropertyControlGrid.Children.Clear();
                PropertyControlGrid.Children.Add(viewModel.CreateControl());
            }
        }

        private void KeyFrameCollectionView_KeyFrameMoveRequest(object sender, KeyFrameMoveEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.MoveTimeKeyFramesCommand.Execute(Tuple.Create(e.KeyFrames, e.NewTimes));
        }

        private void KeyFrameCollectionView_KeyFrameInterpolationTypeChangeRequest(object sender, ChangeKeyFrameInterpolationTypeEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.ChangeKeyFramesInterpolationTypeCommand.Execute(Tuple.Create(e.KeyFrames, e.InterpolationType));
        }

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                var indent = UIParameters.ArrowWidth * propertyView.IndentLevel;
                if (propertyView.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                var nameAreaWidth = propertyView.NameAreaWidth - UIParameters.ArrowWidth * propertyView.IndentLevel + UIParameters.ArrowWidth - KeyFrameSwitchWidth;
                if (propertyView.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                propertyView.IndentMarginLeft = new GridLength(indent);
                propertyView.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
                propertyView.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyView.ControlAreaWidth - indent - nameAreaWidth - KeyFrameSwitchWidth, 0.0));
            }
        }

        static void ControlAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                propertyView.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyView.ControlAreaWidth - propertyView.IndentMarginLeft.Value - propertyView.CalculatedNameAreaWidth.Value - KeyFrameSwitchWidth, 0.0));
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                var nameAreaWidth = propertyView.NameAreaWidth - UIParameters.ArrowWidth * propertyView.IndentLevel + UIParameters.ArrowWidth - KeyFrameSwitchWidth;
                if (propertyView.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                propertyView.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
                propertyView.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyView.ControlAreaWidth - propertyView.IndentMarginLeft.Value - nameAreaWidth - KeyFrameSwitchWidth, 0.0));
            }
        }
    }
}
