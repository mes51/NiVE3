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
    /// SlidableNumberTextBox.xaml の相互作用ロジック
    /// </summary>
    public partial class SlidableNumberTextBox : UserControl
    {
        // TODO: 要調整
        const double SlideStartThreashold = 4.0;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged, CoerceValue)
        );

        public static readonly DependencyProperty IntValueProperty = DependencyProperty.Register(
            nameof(IntValue),
            typeof(int),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IntValueChanged, CoerceIntValue)
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(double.MaxValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, LimitChanged)
        );

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(double.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, LimitChanged)
        );

        public static readonly DependencyProperty SlideChangeValueProperty = DependencyProperty.Register(
            nameof(SlideChangeValue),
            typeof(double),
            typeof(SlidableNumberTextBox),
            new PropertyMetadata(1.0)
        );

        public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(
            nameof(Converter),
            typeof(IValueConverter),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ConverterChanged)
        );

        public static readonly DependencyProperty DigitProperty = DependencyProperty.Register(
            nameof(Digit),
            typeof(int),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, DigitChanged)
        );

        public static readonly DependencyProperty UnitTextProperty = DependencyProperty.Register(
            nameof(UnitText),
            typeof(string),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, UnitTextChanged)
        );

        private static readonly DependencyProperty IsClickedProperty = DependencyProperty.Register(
            nameof(IsClicked),
            typeof(bool),
            typeof(SlidableNumberTextBox),
            new FrameworkPropertyMetadata(false)
        );

        public static RoutedEvent BeginSlideEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(BeginSlideEditValue), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        public static RoutedEvent EndSlideEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(EndSlideEditValue), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        public static RoutedEvent AbortSlideEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(AbortSlideEditValue), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        public static RoutedEvent BeginTextEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(BeginTextEditValue), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        public static RoutedEvent EndTextEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(EndTextEditValue), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        public static RoutedEvent AbortTextEditValueEvent = EventManager.RegisterRoutedEvent(
            nameof(AbortTextEditValue), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SlidableNumberTextBox)
        );

        private bool IsClicked
        {
            get { return (bool)GetValue(IsClickedProperty); }
            set { SetValue(IsClickedProperty, value); }
        }

        public string UnitText
        {
            get { return (string)GetValue(UnitTextProperty); }
            set { SetValue(UnitTextProperty, value); }
        }

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

        bool IsMoved { get; set; }

        bool IsEditingText { get; set; }

        Point ClickPoint { get; set; }

        double PrevValue { get; set; }

        CalcDoubleConverter DefaultConverter { get; } = new CalcDoubleConverter();

        public event RoutedEventHandler AbortTextEditValue
        {
            add { AddHandler(AbortTextEditValueEvent, value); }
            remove { RemoveHandler(AbortTextEditValueEvent, value); }
        }

        public event RoutedEventHandler EndTextEditValue
        {
            add { AddHandler(EndTextEditValueEvent, value); }
            remove { RemoveHandler(EndTextEditValueEvent, value); }
        }

        public event RoutedEventHandler BeginTextEditValue
        {
            add { AddHandler(BeginTextEditValueEvent, value); }
            remove { RemoveHandler(BeginTextEditValueEvent, value); }
        }

        public event RoutedEventHandler AbortSlideEditValue
        {
            add { AddHandler(AbortSlideEditValueEvent, value); }
            remove { RemoveHandler(AbortSlideEditValueEvent, value); }
        }

        public event RoutedEventHandler EndSlideEditValue
        {
            add { AddHandler(EndSlideEditValueEvent, value); }
            remove { RemoveHandler(EndSlideEditValueEvent, value); }
        }

        public event RoutedEventHandler BeginSlideEditValue
        {
            add { AddHandler(BeginSlideEditValueEvent, value); }
            remove { RemoveHandler(BeginSlideEditValueEvent, value); }
        }

        public SlidableNumberTextBox()
        {
            InitializeComponent();

            Converter = DefaultConverter;
            UpdateBinding();
        }

        void UpdateBinding()
        {
            BindingOperations.ClearBinding(ValueTextBlock, TextBlock.TextProperty);
            BindingOperations.ClearBinding(ValueTextBox, TextBox.TextProperty);

            BindingOperations.SetBinding(ValueTextBlock, TextBlock.TextProperty, new Binding(nameof(Value)) { Source = this, Converter = Converter, StringFormat = $"{{0}}{UnitText}" });
            BindingOperations.SetBinding(ValueTextBox, TextBox.TextProperty, new Binding(nameof(Value)) { Source = this, Converter = Converter, UpdateSourceTrigger = UpdateSourceTrigger.Explicit, NotifyOnSourceUpdated = true });
        }

        void ClearFocus()
        {
            var currentFocused = Keyboard.FocusedElement as DependencyObject;
            Keyboard.ClearFocus();
            if (currentFocused == null)
            {
                return;
            }

            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                if (parent is UIElement ui && ui.Focusable)
                {
                    ui.Focus();
                    return;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        private void Root_GotFocus(object sender, RoutedEventArgs e)
        {
            ValueTextBox.SelectAll();
        }

        private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsClicked && e.Key == Key.Escape)
            {
                Mouse.Capture(null);
                ClearFocus();
                Value = PrevValue;
                IsClicked = false;
                e.Handled = true;

                RaiseEvent(new RoutedEventArgs(AbortSlideEditValueEvent, this));
            }
        }

        private void ValueTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                IsClicked = true;
                IsMoved = false;
                PrevValue = Value;
                ClickPoint = e.GetPosition(this);
                ((UIElement)sender).CaptureMouse();
                Focus();
                e.Handled = true;
            }
        }

        private void ValueTextBlock_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsClicked)
            {
                return;
            }

            var newPos = e.GetPosition(this);
            var diff = newPos - ClickPoint;
            if (IsMoved || diff.Length > SlideStartThreashold)
            {
                if (!IsMoved)
                {
                    IsMoved = true;
                    RaiseEvent(new RoutedEventArgs(BeginSlideEditValueEvent, this));
                }

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
                e.Handled = true;
            }
        }

        private void ValueTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
            {
                ClearFocus();
                IsClicked = false;
                ((UIElement)sender).ReleaseMouseCapture();

                if (IsMoved)
                {
                    var diff = e.GetPosition(this) - ClickPoint;
                    if (Math.Abs(diff.X) < 1.0 && Math.Abs(diff.Y) < 1.0)
                    {
                        e.Handled = true;
                        RaiseEvent(new RoutedEventArgs(EndSlideEditValueEvent, this));
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
                    e.Handled = true;

                    RaiseEvent(new RoutedEventArgs(EndSlideEditValueEvent, this));
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
                ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ClearFocus();
                Value = PrevValue;
                e.Handled = true;

                IsEditingText = false;
                RaiseEvent(new RoutedEventArgs(AbortTextEditValueEvent, this));
            }
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PrevValue = Value;
            IsEditingText = true;
            RaiseEvent(new RoutedEventArgs(BeginTextEditValueEvent, this));
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();

            if (Validation.GetHasError(ValueTextBox))
            {
                Value = PrevValue;
                ValueTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();

                IsEditingText = false;
                RaiseEvent(new RoutedEventArgs(AbortTextEditValueEvent, this));
            }
        }

        private void ValueTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (IsEditingText && !ValueTextBox.IsFocused)
            {
                IsEditingText = false;
                RaiseEvent(new RoutedEventArgs(EndTextEditValueEvent, this));
            }
        }

        static void ConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider)
            {
                slider.UpdateBinding();
            }
        }

        static void DigitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider && slider.Converter == slider.DefaultConverter)
            {
                slider.DefaultConverter.Digit = slider.Digit;
                slider.ValueTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                slider.ValueTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            }
        }

        static void LimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider)
            {
                slider.Value = Math.Min(Math.Max(slider.Value, slider.Minimum), slider.Maximum);
            }
        }

        static void IntValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider && slider.IntValue != (int)Math.Min(Math.Max(slider.Value, int.MinValue), int.MaxValue))
            {
                slider.Value = slider.IntValue;
            }
        }

        static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider)
            {
                slider.IntValue = (int)Math.Min(Math.Max(slider.Value, int.MinValue), int.MaxValue);
            }
        }

        static void UnitTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SlidableNumberTextBox slider)
            {
                slider.UpdateBinding();
            }
        }

        static object CoerceIntValue(DependencyObject d, object value)
        {
            if (d is SlidableNumberTextBox slider && value is int v)
            {
                var max = Math.Min(slider.Maximum, int.MaxValue);
                var min = Math.Max(slider.Minimum, int.MinValue);
                return (int)Math.Min(Math.Max(v, min), max);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static object CoerceValue(DependencyObject d, object value)
        {
            if (d is SlidableNumberTextBox slider && value is double v)
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
