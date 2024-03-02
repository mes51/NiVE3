using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NiVE3.Plugin.Property;
using NiVE3.View.Converter;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    public class PropertyViewBase : UserControl
    {
        public static readonly IValueConverter NextIndentConverter = new DelegateConverter<int, int>(v => v + 1);

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, NameAreaWidthChanged)
        );

        protected static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        protected static readonly DependencyProperty CalculatedControlAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedControlAreaWidth),
            typeof(GridLength),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        protected static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ViewStateProperty = DependencyProperty.Register(
            nameof(ViewState),
            typeof(PropertyViewState),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ParentHasExpanderArrowProperty = DependencyProperty.Register(
            nameof(ParentHasExpanderArrow),
            typeof(bool),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty BeforeNameSpaceWidthProperty = DependencyProperty.Register(
            nameof(BeforeNameSpaceWidth),
            typeof(double),
            typeof(PropertyViewBase),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public double BeforeNameSpaceWidth
        {
            get { return (double)GetValue(BeforeNameSpaceWidthProperty); }
            set { SetValue(BeforeNameSpaceWidthProperty, value); }
        }

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

        public bool ParentHasExpanderArrow
        {
            get { return (bool)GetValue(ParentHasExpanderArrowProperty); }
            set { SetValue(ParentHasExpanderArrowProperty, value); }
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

        protected GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        protected GridLength CalculatedControlAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedControlAreaWidthProperty); }
            set { SetValue(CalculatedControlAreaWidthProperty, value); }
        }

        protected GridLength CalculatedNameAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedNameAreaWidthProperty); }
            set { SetValue(CalculatedNameAreaWidthProperty, value); }
        }

        internal PropertyCollectionView? ParentCollection => ItemsControl.ItemsControlFromItemContainer(ParentContainer) as PropertyCollectionView;

        internal FrameworkElement ParentContainer => (FrameworkElement)VisualTreeHelper.GetParent(this);

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyViewBase propertyViewBase)
            {
                var indent = UIParameters.ArrowWidth * propertyViewBase.IndentLevel;
                if (propertyViewBase.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                var nameAreaWidth = propertyViewBase.NameAreaWidth - UIParameters.ArrowWidth * propertyViewBase.IndentLevel + UIParameters.ArrowWidth - propertyViewBase.BeforeNameSpaceWidth;
                if (!propertyViewBase.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (propertyViewBase.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                propertyViewBase.IndentMarginLeft = new GridLength(indent);
                propertyViewBase.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
                propertyViewBase.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyViewBase.ControlAreaWidth - indent - nameAreaWidth - propertyViewBase.BeforeNameSpaceWidth, 0.0));
            }
        }

        static void ControlAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyViewBase propertyViewBase)
            {
                propertyViewBase.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyViewBase.ControlAreaWidth - propertyViewBase.IndentMarginLeft.Value - propertyViewBase.CalculatedNameAreaWidth.Value - propertyViewBase.BeforeNameSpaceWidth, 0.0));
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyViewBase propertyViewBase)
            {
                var nameAreaWidth = propertyViewBase.NameAreaWidth - UIParameters.ArrowWidth * propertyViewBase.IndentLevel + UIParameters.ArrowWidth - propertyViewBase.BeforeNameSpaceWidth;
                if (!propertyViewBase.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (propertyViewBase.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                propertyViewBase.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
                propertyViewBase.CalculatedControlAreaWidth = new GridLength(Math.Max(propertyViewBase.ControlAreaWidth - propertyViewBase.IndentMarginLeft.Value - nameAreaWidth - propertyViewBase.BeforeNameSpaceWidth, 0.0));
            }
        }
    }
}
