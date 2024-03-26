using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    // NOTE: 子要素を持つコントロールはXAMLで作るとBindingが上手くいかなくなる
    // SEE: https://shuntaro3.hatenablog.com/entry/2018/10/01/002603
    class LayerItemExpander : ContentControl
    {
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
            nameof(HeaderText),
            typeof(string),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(0, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(0.0, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty SwitchesProperty = DependencyProperty.Register(
            nameof(Switches),
            typeof(UIElement),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty NameDragSourceIgnoreProperty = DependencyProperty.Register(
            nameof(NameDragSourceIgnore),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty IsHighlightHeaderProperty = DependencyProperty.Register(
            nameof(IsHighlightHeader),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ParentHasExpanderArrowProperty = DependencyProperty.Register(
            nameof(ParentHasExpanderArrow),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty HeaderSubItemAreaWidthProperty = DependencyProperty.Register(
            nameof(HeaderSubItemAreaWidth),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty HeaderSubItemProperty = DependencyProperty.Register(
            nameof(HeaderSubItem),
            typeof(UIElement),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsNameEditingProperty = DependencyProperty.Register(
            nameof(IsNameEditing),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty EndEditNameCommandProperty = DependencyProperty.Register(
            nameof(EndEditNameCommand),
            typeof(ICommand),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public ICommand? EndEditNameCommand
        {
            get { return (ICommand)GetValue(EndEditNameCommandProperty); }
            set { SetValue(EndEditNameCommandProperty, value); }
        }

        public bool IsNameEditing
        {
            get { return (bool)GetValue(IsNameEditingProperty); }
            set { SetValue(IsNameEditingProperty, value); }
        }

        public UIElement? HeaderSubItem
        {
            get { return (UIElement)GetValue(HeaderSubItemProperty); }
            set { SetValue(HeaderSubItemProperty, value); }
        }

        public double ControlAreaWidth
        {
            get { return (double)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        public GridLength HeaderSubItemAreaWidth
        {
            get { return (GridLength)GetValue(HeaderSubItemAreaWidthProperty); }
            set { SetValue(HeaderSubItemAreaWidthProperty, value); }
        }

        public bool IsHighlightHeader
        {
            get { return (bool)GetValue(IsHighlightHeaderProperty); }
            set { SetValue(IsHighlightHeaderProperty, value); }
        }

        public bool NameDragSourceIgnore
        {
            get { return (bool)GetValue(NameDragSourceIgnoreProperty); }
            set { SetValue(NameDragSourceIgnoreProperty, value); }
        }

        public UIElement? Switches
        {
            get { return (UIElement)GetValue(SwitchesProperty); }
            set { SetValue(SwitchesProperty, value); }
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

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public bool ParentHasExpanderArrow
        {
            get { return (bool)GetValue(ParentHasExpanderArrowProperty); }
            set { SetValue(ParentHasExpanderArrowProperty, value); }
        }

        private GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        private GridLength CalculatedNameAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedNameAreaWidthProperty); }
            set { SetValue(CalculatedNameAreaWidthProperty, value); }
        }

        static LayerItemExpander()
        {
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.Inherits));
            IsTabStopProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

            // NOTE: なぜか何かイベントハンドラを渡さないとFocusVisualStyleがnullにならない
            // TODO: 原因の調査
            FocusVisualStyleProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(null, (_, _) => { }));
        }

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerItemExpander expander)
            {
                var indent = UIParameters.ArrowWidth * expander.IndentLevel;
                if (expander.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                expander.IndentMarginLeft = new GridLength(indent);

                var nameAreaWidth = expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth;
                if (!expander.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerItemExpander expander)
            {
                var nameAreaWidth = expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth;
                if (!expander.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
            }
        }
    }
}
