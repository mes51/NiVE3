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
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsShowAVSwitchAreaProperty = DependencyProperty.Register(
            nameof(IsShowAVSwitchArea),
            typeof(bool),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(LayerParameterExpander),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public int IndentLevel
        {
            get { return (int)GetValue(IndentLevelProperty); }
            set { SetValue(IndentLevelProperty, value); }
        }

        public bool IsShowAVSwitchArea
        {
            get { return (bool)GetValue(IsShowAVSwitchAreaProperty); }
            set { SetValue(IsShowAVSwitchAreaProperty, value); }
        }

        private GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerParameterExpander expander)
            {
                var indent = UIParameters.ArrowWidth * expander.IndentLevel;
                if (expander.IsShowAVSwitchArea)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                expander.IndentMarginLeft = new GridLength(indent);
            }
        }
    }
}
