using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Plugin.Property.Control;
using NiVE3.View.Resource;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// PropertyView.xaml の相互作用ロジック
    /// </summary>
    public partial class PropertyView : UserControl
    {
        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsShowAVSwitchAreaProperty = DependencyProperty.Register(
            nameof(IsShowAVSwitchArea),
            typeof(bool),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, NameAreaWidthChanged)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty CalculatedControlAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedControlAreaWidth),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        private static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(new GridLength(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public PropertyControlBase? PropertyControl
        {
            get
            {
                return PropertyControlGrid.Children.OfType<PropertyControlBase>().FirstOrDefault();
            }
            set
            {
                PropertyControlGrid.Children.Clear();
                PropertyControlGrid.Children.Add(value);
            }
        }

        public double NameAreaWidth
        {
            get { return (double)GetValue(NameAreaWidthProperty); }
            set { SetValue(NameAreaWidthProperty, value); }
        }

        public bool IsShowAVSwitchArea
        {
            get { return (bool)GetValue(IsShowAVSwitchAreaProperty); }
            set { SetValue(IsShowAVSwitchAreaProperty, value); }
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

        private GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        private GridLength CalculatedControlAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedControlAreaWidthProperty); }
            set { SetValue(CalculatedControlAreaWidthProperty, value); }
        }

        private GridLength CalculatedNameAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedNameAreaWidthProperty); }
            set { SetValue(CalculatedNameAreaWidthProperty, value); }
        }

        public PropertyView()
        {
            InitializeComponent();
        }

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                var indent = UIParameters.ArrowWidth * propertyView.IndentLevel;
                if (propertyView.IsShowAVSwitchArea)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                var nameAreaWidth = propertyView.NameAreaWidth - UIParameters.ArrowWidth * propertyView.IndentLevel;
                propertyView.IndentMarginLeft = new GridLength(indent);
                propertyView.CalculatedNameAreaWidth = new GridLength(nameAreaWidth);
                propertyView.CalculatedControlAreaWidth = new GridLength(propertyView.ControlAreaWidth - indent - nameAreaWidth);
            }
        }

        static void ControlAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                propertyView.CalculatedControlAreaWidth = new GridLength(propertyView.ControlAreaWidth - propertyView.IndentMarginLeft.Value - propertyView.CalculatedNameAreaWidth.Value);
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyView propertyView)
            {
                var nameAreaWidth = propertyView.NameAreaWidth - UIParameters.ArrowWidth * propertyView.IndentLevel;
                propertyView.CalculatedNameAreaWidth = new GridLength(nameAreaWidth);
                propertyView.CalculatedControlAreaWidth = new GridLength(propertyView.ControlAreaWidth - propertyView.IndentMarginLeft.Value - nameAreaWidth);
            }
        }
    }
}
