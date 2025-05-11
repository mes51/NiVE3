using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.UI.Resources;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class PropertyCollectionView : StackableItemsCollectionView<IInternalPropertyViewModel>
    {
        static readonly IValueConverter VisibilityConverter = new BooleanToVisibilityConverter();

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty ParentHasExpanderArrowProperty = DependencyProperty.Register(
            nameof(ParentHasExpanderArrow),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsAppendablePropertyChildProperty = DependencyProperty.Register(
            nameof(IsAppendablePropertyChild),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty CurrentPropertyGroupProperty = DependencyProperty.Register(
            nameof(CurrentPropertyGroup),
            typeof(IInternalPropertyViewModel),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(null)
        );

        public IInternalPropertyViewModel? CurrentPropertyGroup
        {
            get { return (IInternalPropertyViewModel)GetValue(CurrentPropertyGroupProperty); }
            set { SetValue(CurrentPropertyGroupProperty, value); }
        }

        public bool IsAppendablePropertyChild
        {
            get { return (bool)GetValue(IsAppendablePropertyChildProperty); }
            set { SetValue(IsAppendablePropertyChildProperty, value); }
        }

        public bool ParentHasExpanderArrow
        {
            get { return (bool)GetValue(ParentHasExpanderArrowProperty); }
            set { SetValue(ParentHasExpanderArrowProperty, value); }
        }

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

        static PropertyCollectionView()
        {
            UseItemContextMenuProperty.OverrideMetadata(typeof(PropertyCollectionView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new PropertyContentPresenter();
        }
    }

    class PropertyContentPresenter : ContentPresenter { }
}
