using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using NiVE3.View.Converter;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class LayerCollectionView : StackableItemsCollectionView<LayerViewModel>
    {
        static readonly IValueConverter LayerNumberOffsetConverter = new DelegateConverter<int, int>(v => v + 1);

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(LayerCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(LayerCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(LayerCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

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

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
        }

        public LayerCollectionView()
        {
            AlternationCount = 1000000;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new LayerView();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is LayerView;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is LayerView layer && item is LayerViewModel viewModel)
            {
                layer.DataContext = viewModel;

                var widthBinding = new Binding
                {
                    Path = new PropertyPath(nameof(ControlAreaWidth)),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(layer, LayerView.LayerControlAreaWidthProperty, widthBinding);

                var numberBinding = new Binding
                {
                    Path = new PropertyPath(AlternationIndexProperty),
                    Source = layer,
                    Mode = BindingMode.OneWay,
                    Converter = LayerNumberOffsetConverter
                };
                BindingOperations.SetBinding(layer, LayerView.LayerNumberProperty, numberBinding);

                var rangeBinding = new Binding
                {
                    Path = new PropertyPath(RangeProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(layer, LayerView.RangeProperty, rangeBinding);

                var rangeStartBinding = new Binding
                {
                    Path = new PropertyPath(RangeStartProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(layer, LayerView.RangeStartProperty, rangeStartBinding);

                var compositionFrameRateBinding = new Binding
                {
                    Path = new PropertyPath(CompositionFrameRateProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(layer, LayerView.CompositionFrameRateProperty, compositionFrameRateBinding);

                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(layer, true);
                GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(layer, viewModel);
            }
        }
    }
}
