using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NiVE3.Plugin.Property;
using NiVE3.UI.Resources;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class PropertyCollectionView : StackableItemsCollectionView<PropertyViewModel>
    {
        static IValueConverter VisibilityConverter = new BooleanToVisibilityConverter();

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
            typeof(double),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(0.0)
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

        public PropertyCollectionView()
        {
            var templateSelector = new DataTemplateCollection();
            templateSelector.Templates.Add(new DataTemplate
            {
                DataType = typeof(PropertyViewModel),
                VisualTree = CreateControlFactory(typeof(PropertyView))
            });
            templateSelector.Templates.Add(new DataTemplate
            {
                DataType = typeof(AppendablePropertyViewModel),
                VisualTree = CreateControlFactory(typeof(AppendablePropertyView))
            });
            templateSelector.Templates.Add(new DataTemplate
            {
                DataType = typeof(PropertyGroupViewModel),
                VisualTree = CreateControlFactory(typeof(PropertyGroupView))
            });

            ItemTemplateSelector = templateSelector;
        }

        FrameworkElementFactory CreateControlFactory(Type uiType)
        {
            var factory = new FrameworkElementFactory(uiType);
            var controlAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(ControlAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.ControlAreaWidthProperty, controlAreaWidthBinding);

            var nameAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(NameAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.NameAreaWidthProperty, nameAreaWidthBinding);

            var indentLevelBinding = new Binding
            {
                Path = new PropertyPath(nameof(IndentLevel)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.IndentLevelProperty, indentLevelBinding);

            var isAVSwitchColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsAVSwitchColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.IsAVSwitchColumnVisibleProperty, isAVSwitchColumnVisibleBinding);

            var isTagColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsTagColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.IsTagColumnVisibleProperty, isTagColumnVisibleBinding);

            var viewStateBinding = new Binding
            {
                Path = new PropertyPath(nameof(PropertyViewModel.ViewState)),
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.ViewStateProperty, viewStateBinding);

            var visibilityBinding = new Binding
            {
                Path = new PropertyPath($"{nameof(PropertyViewModel.ViewState)}.{nameof(PropertyViewState.IsVisible)}"),
                Mode = BindingMode.OneWay,
                Converter = VisibilityConverter
            };
            factory.SetBinding(VisibilityProperty, visibilityBinding);

            var isEnabledBinding = new Binding
            {
                Path = new PropertyPath($"{nameof(PropertyViewModel.ViewState)}.{nameof(PropertyViewState.IsEnabled)}"),
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(IsEnabledProperty, isEnabledBinding);

            var rangeBinding = new Binding
            {
                Path = new PropertyPath(nameof(Range)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.RangeProperty, rangeBinding);

            var rangeStartBinding = new Binding
            {
                Path = new PropertyPath(nameof(RangeStart)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.RangeStartProperty, rangeStartBinding);

            var compositionFrameRateBinding = new Binding
            {
                Path = new PropertyPath(nameof(CompositionFrameRate)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.CompositionFrameRateProperty, compositionFrameRateBinding);

            var parentHasExpanderArrowBinding = new Binding
            {
                Path = new PropertyPath(nameof(ParentHasExpanderArrow)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(PropertyViewBase.ParentHasExpanderArrowProperty, parentHasExpanderArrowBinding);

            return factory;
        }
    }
}
