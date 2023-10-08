using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// NumericUpDown.xaml の相互作用ロジック
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged)
        );

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata((double)int.MaxValue, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged)
        );

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged)
        );

        public static readonly DependencyProperty DecimalDigitProperty = DependencyProperty.Register(
            nameof(DecimalDigit),
            typeof(int),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged)
        );

        public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
            nameof(ValueText),
            typeof(string),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata("0", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, ValueTextChanged)
        );

        static readonly Regex DoubleRegex = new Regex("^-?([0-9]+[Ee]-?[0-9]+|[0-9]*\\.?[0-9]+)", RegexOptions.Compiled);

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(nameof(ValuePropertyChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumericUpDown));

        public NumericUpDown()
        {
            InitializeComponent();
        }

        public double Value
        {
            get { return Math.Max(Math.Min(Math.Round((double)GetValue(ValueProperty), DecimalDigit), MaxValue), MinValue); }
            set { SetValue(ValueProperty, value); }
        }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public int DecimalDigit
        {
            get { return (int)GetValue(DecimalDigitProperty); }
            set { SetValue(DecimalDigitProperty, value); }
        }

        public string ValueText
        {
            get { return (string)GetValue(ValueTextProperty); }
            set { SetValue(ValueTextProperty, value); }
        }

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        void UpdateText()
        {
            ValueText = Value.ToString($"F{DecimalDigit}");
        }

        void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value += Math.Pow(0.1, DecimalDigit);
        }

        void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value -= Math.Pow(0.1, DecimalDigit);
        }

        static void ValuePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is NumericUpDown numericUpDown)
            {
                numericUpDown.UpButton.IsEnabled = numericUpDown.Value != numericUpDown.MaxValue;
                numericUpDown.DownButton.IsEnabled = numericUpDown.Value != numericUpDown.MinValue;
                numericUpDown.UpdateText();
                numericUpDown.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }
        }

        static void ValueTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is NumericUpDown numericUpDown)
            {
                var numberText = DoubleRegex.Match(e.NewValue as string ?? "");
                double result;
                if (numberText.Success && double.TryParse(numberText.Value, out result) && numericUpDown.Value != result)
                {
                    numericUpDown.Value = result;
                }
            }
        }
    }
}
