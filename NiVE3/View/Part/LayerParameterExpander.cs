using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows.Controls;
using System.Windows;

namespace NiVE3.View.Part
{
    class LayerParameterExpander : Expander
    {
        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(0, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(true, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(0.0, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty SwitchesProperty = DependencyProperty.Register(
            nameof(Switches),
            typeof(UIElement),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty NameDragSourceIgnoreProperty = DependencyProperty.Register(
            nameof(NameDragSourceIgnore),
            typeof(bool),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(true)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

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

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerParameterExpander expander)
            {
                var indent = UIParameters.ArrowWidth * expander.IndentLevel;
                if (expander.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                expander.IndentMarginLeft = new GridLength(indent);

                var nameAreaWidth = Math.Max(expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth, 0.0);
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(nameAreaWidth);
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerParameterExpander expander)
            {
                var nameAreaWidth = Math.Max(expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth, 0.0);
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(nameAreaWidth);
            }
        }
    }
}
