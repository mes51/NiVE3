using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NiVE3.View.Converter;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class LayerCollectionView : NestableItemsCollectionView<LayerViewModel>
    {
        static readonly DelegateMultiValueConverter<bool, bool, bool> CanDragConverter = new DelegateMultiValueConverter<bool, bool, bool>((v1, v2) => !(v1 || v2));

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

        public static readonly DependencyProperty IsTimeAreaEditingProperty = DependencyProperty.Register(
            nameof(IsTimeAreaEditing),
            typeof(bool),
            typeof(LayerCollectionView),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty IsLayerEditingProperty = DependencyProperty.Register(
            nameof(IsLayerEditing),
            typeof(bool),
            typeof(LayerCollectionView),
            new FrameworkPropertyMetadata(false)
        );

        public bool IsLayerEditing
        {
            get { return (bool)GetValue(IsLayerEditingProperty); }
            set { SetValue(IsLayerEditingProperty, value); }
        }

        public bool IsTimeAreaEditing
        {
            get { return (bool)GetValue(IsTimeAreaEditingProperty); }
            set { SetValue(IsTimeAreaEditingProperty, value); }
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

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
        }

        public LayerCollectionView()
        {
            AlternationCount = 1000000;

            var isDragSourceBinding = new MultiBinding
            {
                Converter = CanDragConverter,
                Mode = BindingMode.OneWay
            };
            isDragSourceBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(IsTimeAreaEditingProperty),
                Source = this,
                Mode = BindingMode.OneWay
            });
            isDragSourceBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(IsLayerEditingProperty),
                Source = this,
                Mode = BindingMode.OneWay
            });
            BindingOperations.SetBinding(this, GongSolutions.Wpf.DragDrop.DragDrop.IsDragSourceProperty, isDragSourceBinding);

            var isDropTargetBinding = new MultiBinding
            {
                Converter = CanDragConverter,
                Mode = BindingMode.OneWay
            };
            isDropTargetBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(IsTimeAreaEditingProperty),
                Source = this,
                Mode = BindingMode.OneWay
            });
            isDropTargetBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath(IsLayerEditingProperty),
                Source = this,
                Mode = BindingMode.OneWay
            });
            BindingOperations.SetBinding(this, GongSolutions.Wpf.DragDrop.DragDrop.IsDropTargetProperty, isDropTargetBinding);
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
                    Converter = new DelegateConverter<int, int>(v => v + 1)
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

                layer.IsDurationEditingChanged += Layer_IsDurationEditingChanged;
                Mouse.AddGotMouseCaptureHandler(layer, Layer_MouseCaptured);
                Mouse.AddLostMouseCaptureHandler(layer, Layer_MouseCaptureReleased);
            }
        }

        private void Layer_MouseCaptured(object? sender, MouseEventArgs e)
        {
            IsLayerEditing = true;
        }

        private void Layer_MouseCaptureReleased(object? sender, MouseEventArgs e)
        {
            IsLayerEditing = false;
        }

        private void Layer_IsDurationEditingChanged(object? sender, EventArgs e)
        {
            IsTimeAreaEditing = ItemsSource?.OfType<object>()
                .Select(vm => ItemContainerGenerator.ContainerFromItem(vm))
                .OfType<LayerView>()
                .Any(l => l.IsDurationEditing) ?? false;
        }
    }
}
