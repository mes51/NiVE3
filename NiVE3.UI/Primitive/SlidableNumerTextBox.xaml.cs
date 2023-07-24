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
using NiVE3.UI.Converter;

namespace NiVE3.UI.Primitive
{
    /// <summary>
    /// SlidableNumerTextBox.xaml の相互作用ロジック
    /// </summary>
    public partial class SlidableNumerTextBox : UserControl
    {
        // TODO: 要調整
        const double SlideStartThreashold = 4.0;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged, CoerceValue)
        );

        public static readonly DependencyProperty IntValueProperty = DependencyProperty.Register(
            nameof(IntValue),
            typeof(int),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IntValueChanged, CoerceIntValue)
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(double.MaxValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, LimitChanged)
        );

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(double.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, LimitChanged)
        );

        public static readonly DependencyProperty SlideChangeValueProperty = DependencyProperty.Register(
            nameof(SlideChangeValue),
            typeof(double),
            typeof(SlidableNumerTextBox),
            new PropertyMetadata(1.0)
        );

        public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(
            nameof(Converter),
            typeof(IValueConverter),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ConverterChanged)
        );

        public static readonly DependencyProperty DigitProperty = DependencyProperty.Register(
            nameof(Digit),
            typeof(int),
            typeof(SlidableNumerTextBox),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, DigitChanged)
        );

        public int Digit
        {
            get { return (int)GetValue(DigitProperty); }
            set { SetValue(DigitProperty, value); }
        }

        public IValueConverter Converter
        {
            get { return (IValueConverter)GetValue(ConverterProperty); }
            set { SetValue(ConverterProperty, value); }
        }

        public double SlideChangeValue
        {
            get { return (double)GetValue(SlideChangeValueProperty); }
            set { SetValue(SlideChangeValueProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public int IntValue
        {
            get { return (int)GetValue(IntValueProperty); }
            set { SetValue(IntValueProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        bool IsClick { get; set; }

        bool IsMoved { get; set; }

        Point ClickPoint { get; set; }

        double PrevValue { get; set; }

        CalcDoubleConverter DefaultConverter { get; } = new CalcDoubleConverter();

        public SlidableNumerTextBox()
        {
            InitializeComponent();

            Converter = DefaultConverter;
            UpdateBinding();
        }

        void UpdateBinding()
        {
            BindingOperations.ClearBinding(ValueTextBlock, TextBlock.TextProperty);
            BindingOperations.ClearBinding(ValueTextBox, TextBox.TextProperty);

            BindingOperations.SetBinding(ValueTextBlock, TextBlock.TextProperty, new Binding(nameof(Value)) { Source = this,    Converter = Converter });
            BindingOperations.SetBinding(ValueTextBox, TextBox.TextProperty, new Binding(nameof(Value)) { Source = this, Converter = Converter });
        }

        private void Root_GotFocus(object sender, RoutedEventArgs e)
        {
            ValueTextBox.SelectAll();
        }

        private void ValueTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                IsClick = true;
                IsMoved = false;
                PrevValue = Value;
                ClickPoint = e.GetPosition(this);
                ((UIElement)sender).CaptureMouse();
            }
        }

        private void ValueTextBlock_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsClick)
            {
                return;
            }

            var newPos = e.GetPosition(this);
            var diff = newPos - ClickPoint;
            if (IsMoved || diff.Length > SlideStartThreashold)
            {
                IsMoved = true;

                if (Math.Abs(diff.X) < 1.0 && Math.Abs(diff.Y) < 1.0)
                {
                    return;
                }

                var rate = 1.0;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    rate = 10.0;
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    rate = 0.1;
                }
                // NOTE: CoerceValueでClampされる前の値がBindingで伝播してしまうため、Clampした値をセットする
                Value = Math.Clamp(Value + ((int)diff.X + (int)diff.Y) * SlideChangeValue * rate, Minimum, Maximum);
                ClickPoint = newPos;
            }
        }

        private void ValueTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
            {
                IsClick = false;
                ((UIElement)sender).ReleaseMouseCapture();

                if (IsMoved)
                {
                    var diff = e.GetPosition(this) - ClickPoint;
                    if (Math.Abs(diff.X) < 1.0 && Math.Abs(diff.Y) < 1.0)
                    {
                        return;
                    }

                    var rate = 1.0;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        rate = 10.0;
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        rate = 0.1;
                    }
                    // NOTE: CoerceValueでClampされる前の値がBindingで伝播してしまうため、Clampした値をセットする
                    Value = Math.Clamp(Value + ((int)diff.X + (int)diff.Y) * SlideChangeValue * rate, Minimum, Maximum);
                }
                else
                {
                    Focus();
                }
            }
        }

        private void ValueTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)
            {
                Keyboard.ClearFocus();
                if (Validation.GetHasError(ValueTextBox))
                {
                    Value = PrevValue;
                    ValueTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                Value = PrevValue;
                e.Handled = true;
            }
        }

        static void ConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumerTextBox slider)
            {
                slider.UpdateBinding();
            }
        }

        static void DigitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumerTextBox slider && slider.Converter == slider.DefaultConverter)
            {
                slider.DefaultConverter.Digit = slider.Digit;
                slider.ValueTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                slider.ValueTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            }
        }

        static void LimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumerTextBox slider)
            {
                slider.Value = Math.Min(Math.Max(slider.Value, slider.Minimum), slider.Maximum);
            }
        }

        static void IntValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumerTextBox slider && slider.IntValue != (int)slider.Value)
            {
                slider.Value = slider.IntValue;
            }
        }

        static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumerTextBox slider)
            {
                slider.IntValue = (int)slider.Value;
            }
        }

        static object CoerceIntValue(DependencyObject d, object value)
        {
            if (d is SlidableNumerTextBox slider && value is int v)
            {
                return Math.Min(Math.Max(v, ((int)slider.Minimum)), ((int)slider.Maximum));
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static object CoerceValue(DependencyObject d, object value)
        {
            if (d is SlidableNumerTextBox slider && value is double v)
            {
                return Math.Min(Math.Max(v, slider.Minimum), slider.Maximum);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
