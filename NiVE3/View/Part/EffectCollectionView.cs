using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class EffectCollectionView : StackableItemsCollectionView<EffectViewModel>
    {
        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty IsCommentColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsCommentColumnVisible),
            typeof(bool),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty CommentAreaWidthProperty = DependencyProperty.Register(
            nameof(CommentAreaWidth),
            typeof(double),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(Time.Zero)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(EffectCollectionView),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty ParentHasExpanderArrowProperty = DependencyProperty.Register(
            nameof(ParentHasExpanderArrow),
            typeof(bool),
            typeof(EffectCollectionView),
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

        public double CommentAreaWidth
        {
            get { return (double)GetValue(CommentAreaWidthProperty); }
            set { SetValue(CommentAreaWidthProperty, value); }
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
            return new EffectView();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is EffectView;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is EffectView effect && item is EffectViewModel viewModel)
            {
                effect.DataContext = viewModel;

                var indentLevelBinding = new Binding
                {
                    Path = new PropertyPath(IndentLevelProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.IndentLevelProperty, indentLevelBinding);

                var isAVSwitchColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsAVSwitchColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.IsAVSwitchColumnVisibleProperty, isAVSwitchColumnVisibleBinding);

                var isTagColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsTagColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.IsTagColumnVisibleProperty, isTagColumnVisibleBinding);

                var isCommentColumnVisibleBinding = new Binding
                {
                    Path = new PropertyPath(IsCommentColumnVisibleProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.IsCommentColumnVisibleProperty, isCommentColumnVisibleBinding);

                var controlAreaWidthBinding = new Binding
                {
                    Path = new PropertyPath(ControlAreaWidthProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.ControlAreaWidthProperty, controlAreaWidthBinding);

                var commentAreaWidthBinding = new Binding
                {
                    Path = new PropertyPath(CommentAreaWidthProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.CommentAreaWidthProperty, commentAreaWidthBinding);

                var nameAreaWidthBinding = new Binding
                {
                    Path = new PropertyPath(NameAreaWidthProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.NameAreaWidthProperty, nameAreaWidthBinding);

                var rangeBinding = new Binding
                {
                    Path = new PropertyPath(RangeProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.RangeProperty, rangeBinding);

                var rangeStartBinding = new Binding
                {
                    Path = new PropertyPath(RangeStartProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.RangeStartProperty, rangeStartBinding);

                var compositionFrameRateBinding = new Binding
                {
                    Path = new PropertyPath(CompositionFrameRateProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.CompositionFrameRateProperty, compositionFrameRateBinding);

                var parentHasExpanderArrowBinding = new Binding
                {
                    Path = new PropertyPath(ParentHasExpanderArrowProperty),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(effect, EffectView.ParentHasExpanderArrowProperty, parentHasExpanderArrowBinding);
            }
        }
    }
}
