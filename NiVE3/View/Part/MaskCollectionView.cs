using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class MaskCollectionView : StackableItemsCollectionView<MaskViewModel>
    {
        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty IsCommentColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsCommentColumnVisible),
            typeof(bool),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(MaskCollectionView),
            new FrameworkPropertyMetadata(0.0)
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

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MaskView();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MaskView;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is MaskView mask && item is MaskViewModel viewModel)
            {
                mask.DataContext = viewModel;

                var indentLevelBinding = new Binding
                {
                    Path = new PropertyPath(IndentLevelProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.IndentLevelProperty, indentLevelBinding);

                var isAVSwitchColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsAVSwitchColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.IsAVSwitchColumnVisibleProperty, isAVSwitchColumnVisibleBinding);

                var isTagColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsTagColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.IsTagColumnVisibleProperty, isTagColumnVisibleBinding);

                var isCommentColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsCommentColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.IsCommentColumnVisibleProperty, isCommentColumnVisibleBinding);

                var controlAreaWidthBinding = new Binding
                {
                    Path = new PropertyPath(ControlAreaWidthProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.ControlAreaWidthProperty, controlAreaWidthBinding);

                var nameAreaWidthBinding = new Binding
                {
                    Path = new PropertyPath(NameAreaWidthProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.NameAreaWidthProperty, nameAreaWidthBinding);

                var rangeBinding = new Binding
                {
                    Path = new PropertyPath(RangeProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.RangeProperty, rangeBinding);

                var rangeStartBinding = new Binding
                {
                    Path = new PropertyPath(RangeStartProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.RangeStartProperty, rangeStartBinding);

                var compositionFrameRateBinding = new Binding
                {
                    Path = new PropertyPath(CompositionFrameRateProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(mask, MaskView.CompositionFrameRateProperty, compositionFrameRateBinding);
            }
        }
    }
}
