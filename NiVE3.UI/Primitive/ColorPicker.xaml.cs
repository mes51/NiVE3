using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Image.Color;
using NiVE3.Shared.Extension;

namespace NiVE3.UI.Primitive
{
    /// <summary>
    /// ColorPicker.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static readonly DependencyProperty OldColorProperty = DependencyProperty.Register(
            nameof(OldColor),
            typeof(Color),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(Colors.Black, OldColorChanged)
        );

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColorChanged)
        );

        public static readonly DependencyProperty VectorColorProperty = DependencyProperty.Register(
            nameof(VectorColor),
            typeof(Vector4),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(Vector4.UnitW, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, VectorColorChanged)
        );

        internal static readonly DependencyProperty ColorPickTypeProperty = DependencyProperty.Register(
            nameof(ColorPickType),
            typeof(ColorPickType),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(ColorPickType.H, ColorPickTypeChanged)
        );

        internal static readonly DependencyProperty HueProperty = DependencyProperty.Register(
            nameof(Hue),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(0.0, HSV_ValueChanged)
        );

        internal static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
            nameof(Saturation),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(100.0, HSV_ValueChanged)
        );

        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(100.0, HSV_ValueChanged)
        );

        internal static readonly DependencyProperty RedProperty = DependencyProperty.Register(
            nameof(Red),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(255.0, RGB_ValueChanged)
        );

        internal static readonly DependencyProperty GreenProperty = DependencyProperty.Register(
            nameof(Green),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(255.0, RGB_ValueChanged)
        );

        internal static readonly DependencyProperty BlueProperty = DependencyProperty.Register(
            nameof(Blue),
            typeof(double),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(255.0, RGB_ValueChanged)
        );

        internal double Blue
        {
            get { return (double)GetValue(BlueProperty); }
            set { SetValue(BlueProperty, value); }
        }

        internal double Green
        {
            get { return (double)GetValue(GreenProperty); }
            set { SetValue(GreenProperty, value); }
        }

        internal double Red
        {
            get { return (double)GetValue(RedProperty); }
            set { SetValue(RedProperty, value); }
        }

        internal double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        internal double Saturation
        {
            get { return (double)GetValue(SaturationProperty); }
            set { SetValue(SaturationProperty, value); }
        }

        internal double Hue
        {
            get { return (double)GetValue(HueProperty); }
            set { SetValue(HueProperty, value); }
        }

        internal ColorPickType ColorPickType
        {
            get { return (ColorPickType)GetValue(ColorPickTypeProperty); }
            set { SetValue(ColorPickTypeProperty, value); }
        }

        public Vector4 VectorColor
        {
            get { return (Vector4)GetValue(VectorColorProperty); }
            set { SetValue(VectorColorProperty, value); }
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public Color OldColor
        {
            get { return (Color)GetValue(OldColorProperty); }
            set { SetValue(OldColorProperty, value); }
        }

        public WriteableBitmap ColorPickAreaImage { get; set; } = new WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null);

        public WriteableBitmap ColorBarImage { get; set; } = new WriteableBitmap(24, 256, 96.0, 96.0, PixelFormats.Bgra32, null);

        bool IsClickPickerArea { get; set; }

        bool IsClickColorBar { get; set; }

        bool IsValueChanging { get; set; }

        byte[] ColorPickAreaImageData { get; } = Enumerable.Repeat<byte>(255, 256 * 256 * 4).ToArray();

        byte[] ColorBarImageData { get; } = Enumerable.Repeat<byte>(255, 24 * 256 * 4).ToArray();

        Hsv Hsv => new Hsv((float)Hue, (float)(Saturation * 0.01), (float)(Value * 0.01));

        public ColorPicker()
        {
            InitializeComponent();
        }

        void UpdateColorImage()
        {
            if (ColorPickArea.ActualWidth < 1.0 || ColorPickArea.ActualHeight < 1.0 || ColorBar.ActualHeight < 1.0)
            {
                return;
            }

            Func<int, int, Vector4> areaFunc;
            Func<int, Vector4> barFunc;
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    {
                        var addX = 360.0 / ColorPickAreaImage.Width;
                        var addY = 1.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) =>
                        {
                            var hsv = Hsv;
                            hsv.Hue = (float)(addX * x);
                            hsv.Value = (float)(1.0 - addY * y);
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            var hsv = Hsv;
                            hsv.Saturation = (float)(1.0 - (y / ColorBarImage.Height));
                            return hsv.ToRgb();
                        };
                    }
                    break;
                case ColorPickType.V:
                    {
                        var addX = 360.0 / ColorPickAreaImage.Width;
                        var addY = 1.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) =>
                        {
                            var hsv = Hsv;
                            hsv.Hue = (float)(addX * x);
                            hsv.Saturation = (float)(1.0 - addY * y);
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            var hsv = Hsv;
                            hsv.Value = (float)(1.0 - (y / ColorBarImage.Height));
                            return hsv.ToRgb();
                        };
                    }
                    break;
                case ColorPickType.R:
                    {
                        var addX = 255.0 / ColorPickAreaImage.Width;
                        var addY = 255.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) => Color.FromRgb(Color.R, (byte)Math.Round(255.0 - addY * y), (byte)Math.Round(addX * x)).ToVector4();
                        barFunc = y => Color.FromRgb((byte)Math.Round(255.0 - (y / ColorBarImage.Height) * 255.0), Color.G, Color.B).ToVector4();
                    }
                    break;
                case ColorPickType.G:
                    {
                        var addX = 255.0 / ColorPickAreaImage.Width;
                        var addY = 255.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) => Color.FromRgb((byte)Math.Round(255.0 - addY * y), Color.G, (byte)Math.Round(addX * x)).ToVector4();
                        barFunc = y => Color.FromRgb(Color.R, (byte)Math.Round(255.0 - (y / ColorBarImage.Height) * 255.0), Color.B).ToVector4();
                    }
                    break;
                case ColorPickType.B:
                    {
                        var addX = 255.0 / ColorPickAreaImage.Width;
                        var addY = 255.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) => Color.FromRgb((byte)Math.Round(addX * x), (byte)Math.Round(255.0 - addY * y), Color.B).ToVector4();
                        barFunc = y => Color.FromRgb(Color.R, Color.G, (byte)Math.Round(255.0 - (y / ColorBarImage.Height) * 255.0)).ToVector4();
                    }
                    break;
                default:
                    {
                        var addX = 1.0 / ColorPickAreaImage.Width;
                        var addY = 1.0 / ColorPickAreaImage.Height;
                        areaFunc = (x, y) =>
                        {
                            var hsv = Hsv;
                            hsv.Saturation = (float)(addX * x);
                            hsv.Value = (float)(1.0 - addY * y);
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            return new Hsv((float)(360.0 - (y / ColorBarImage.Height) * 360.0), 1.0F, 1.0F).ToRgb();
                        };
                    }
                    break;
            }

            var pickAreaRect = new Int32Rect(0, 0, (int)ColorPickAreaImage.Width, (int)ColorPickAreaImage.Height);
            var barRect = new Int32Rect(0, 0, (int)ColorBarImage.Width, (int)ColorBarImage.Height);
            for (int y = 0, pos = 0; y < pickAreaRect.Height; y++)
            {
                for (var x = 0; x < pickAreaRect.Width; x++, pos += 4)
                {
                    var color = areaFunc(x, y).ToColor();
                    ColorPickAreaImageData[pos] = color.B;
                    ColorPickAreaImageData[pos + 1] = color.G;
                    ColorPickAreaImageData[pos + 2] = color.R;
                }
            }

            for (int y = 0, pos = 0; y < barRect.Height; y++)
            {
                var color = barFunc(y).ToColor();
                for (var x = 0; x < barRect.Width; x++, pos += 4)
                {
                    ColorBarImageData[pos] = color.B;
                    ColorBarImageData[pos + 1] = color.G;
                    ColorBarImageData[pos + 2] = color.R;
                }
            }

            ColorPickAreaImage.WritePixels(pickAreaRect, ColorPickAreaImageData, pickAreaRect.Width * 4, 0);
            ColorBarImage.WritePixels(barRect, ColorBarImageData, barRect.Width * 4, 0);
        }

        void SetSelectedPosition()
        {
            var circleX = 0.0;
            var circleY = 0.0;
            var barY = 0.0;
            var hsv = Hsv;
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    circleX = hsv.Hue / 360.0 * ColorPickArea.ActualWidth;
                    circleY = (1.0 - hsv.Value) * ColorPickArea.ActualHeight;
                    barY = (1.0 - hsv.Saturation) * ColorBar.ActualHeight;
                    break;
                case ColorPickType.V:
                    circleX = hsv.Hue / 360.0 * ColorPickArea.ActualWidth;
                    circleY = (1.0 - hsv.Saturation) * ColorPickArea.ActualHeight;
                    barY = (1.0 - hsv.Value) * ColorBar.ActualHeight;
                    break;
                case ColorPickType.R:
                    circleX = Color.B / 255.0 * ColorPickArea.ActualWidth;
                    circleY = (255.0 - Color.G) / 255.0 * ColorPickArea.ActualHeight;
                    barY = (255.0 - Color.R) / 255.0 * ColorBar.ActualHeight;
                    break;
                case ColorPickType.G:
                    circleX = Color.B / 255.0 * ColorPickArea.ActualWidth;
                    circleY = (255.0 - Color.R) / 255.0 * ColorPickArea.ActualHeight;
                    barY = (255.0 - Color.G) / 255.0 * ColorBar.ActualHeight;
                    break;
                case ColorPickType.B:
                    circleX = Color.R / 255.0 * ColorPickArea.ActualWidth;
                    circleY = (255.0 - Color.G) / 255.0 * ColorPickArea.ActualHeight;
                    barY = (255.0 - Color.B) / 255.0 * ColorBar.ActualHeight;
                    break;
                default:
                    circleX = hsv.Saturation * ColorPickArea.ActualWidth;
                    circleY = (1.0 - hsv.Value) * ColorPickArea.ActualHeight;
                    barY = (360.0 - hsv.Hue) / 360.0 * ColorBar.ActualHeight;
                    break;
            }
            Canvas.SetLeft(SelectedColorCircle, circleX);
            Canvas.SetTop(SelectedColorCircle, circleY);
            Canvas.SetTop(SelectedColorBarArrow, barY - 2);
        }

        void ChangeColor(Color color, object? inputSource)
        {
            IsValueChanging = true;

            Color = color;
            var hsv = Hsv.FromRgb(color.ToVector4());
            if (inputSource != HueProperty)
            {
                SetCurrentValue(HueProperty, (double)hsv.Hue);
            }
            if (inputSource != SaturationProperty)
            {
                SetCurrentValue(SaturationProperty, hsv.Saturation * 100.0);
            }
            if (inputSource != ValueProperty)
            {
                SetCurrentValue(ValueProperty, hsv.Value * 100.0);
            }
            if (inputSource != RedProperty)
            {
                SetCurrentValue(RedProperty, (double)Color.R);
            }
            if (inputSource != GreenProperty)
            {
                SetCurrentValue(GreenProperty, (double)Color.G);
            }
            if (inputSource != BlueProperty)
            {
                SetCurrentValue(BlueProperty, (double)Color.B);
            }
            if (inputSource != ColorCode)
            {
                ColorCode.Text = Color.ToHex();
            }

            IsValueChanging = false;

            UpdateColorImage();
            SetSelectedPosition();
        }

        void ChangeColorByHSV(Hsv hsv, object? inputSource)
        {
            IsValueChanging = true;

            Color = hsv.ToRgb().ToColor();
            if (inputSource != HueProperty)
            {
                SetCurrentValue(HueProperty, (double)hsv.Hue);
            }
            if (inputSource != SaturationProperty)
            {
                SetCurrentValue(SaturationProperty, hsv.Saturation * 100.0);
            }
            if (inputSource != ValueProperty)
            {
                SetCurrentValue(ValueProperty, hsv.Value * 100.0);
            }
            if (inputSource != RedProperty)
            {
                SetCurrentValue(RedProperty, (double)Color.R);
            }
            if (inputSource != GreenProperty)
            {
                SetCurrentValue(GreenProperty, (double)Color.G);
            }
            if (inputSource != BlueProperty)
            {
                SetCurrentValue(BlueProperty, (double)Color.B);
            }
            if (inputSource != ColorCode)
            {
                ColorCode.Text = Color.ToHex();
            }

            IsValueChanging = false;

            UpdateColorImage();
            SetSelectedPosition();
        }

        void SelectColorFromPickerArea(double x, double y)
        {
            if (ColorPickArea.ActualWidth < 1.0 || ColorPickArea.ActualHeight < 1.0)
            {
                return;
            }

            x = Math.Min(Math.Max(x, 0.0), ColorPickArea.ActualWidth);
            y = Math.Min(Math.Max(y, 0.0), ColorPickArea.ActualHeight);
            var hsv = Hsv;
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    hsv.Hue = (float)(x / ColorPickArea.ActualWidth * 360.0);
                    hsv.Value = (float)(1.0 - y / ColorPickArea.ActualHeight);
                    ChangeColorByHSV(hsv, null);
                    break;
                case ColorPickType.V:
                    hsv.Hue = (float)(x / ColorPickArea.ActualWidth * 360.0);
                    hsv.Saturation = (float)(1.0 - y / ColorPickArea.ActualHeight);
                    ChangeColorByHSV(hsv, null);
                    break;
                case ColorPickType.R:
                    ChangeColor(Color.FromRgb(Color.R, RoundToEvenByte(255.0 - y / ColorPickArea.ActualHeight * 255.0), RoundToEvenByte(x / ColorPickArea.ActualWidth * 255.0)), null);
                    break;
                case ColorPickType.G:
                    ChangeColor(Color.FromRgb(RoundToEvenByte(255.0 - y / ColorPickArea.ActualHeight * 255.0), Color.G, RoundToEvenByte(x / ColorPickArea.ActualWidth * 255.0)), null);
                    break;
                case ColorPickType.B:
                    ChangeColor(Color.FromRgb(RoundToEvenByte(x / ColorPickArea.ActualWidth * 255.0), RoundToEvenByte(255.0F - y / ColorPickArea.ActualHeight * 255.0), Color.B), null);
                    break;
                default:
                    hsv.Saturation = (float)(x / ColorPickArea.ActualWidth);
                    hsv.Value = (float)(1.0 - y / ColorPickArea.ActualHeight);
                    ChangeColorByHSV(hsv, null);
                    break;
            }
        }

        void SelectColorFromColorBar(double y)
        {
            if (ColorBar.ActualHeight < 1.0)
            {
                return;
            }

            y = Math.Min(Math.Max(y, 0.0), ColorBar.ActualHeight);
            var hsv = Hsv;
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    hsv.Saturation = (float)(1.0 - y / ColorBar.ActualHeight);
                    ChangeColorByHSV(hsv, null);
                    break;
                case ColorPickType.V:
                    hsv.Value = (float)(1.0 - y / ColorBar.ActualHeight);
                    ChangeColorByHSV(hsv, null);
                    break;
                case ColorPickType.R:
                    ChangeColor(Color.FromRgb(RoundToEvenByte(255.0 - y / ColorBar.ActualHeight * 255.0), Color.G, Color.B), null);
                    break;
                case ColorPickType.G:
                    ChangeColor(Color.FromRgb(Color.R, RoundToEvenByte(255.0 - y / ColorBar.ActualHeight * 255.0), Color.B), null);
                    break;
                case ColorPickType.B:
                    ChangeColor(Color.FromRgb(Color.R, Color.G, RoundToEvenByte(255.0 - y / ColorBar.ActualHeight * 255.0)), null);
                    break;
                default:
                    hsv.Hue = (float)(360.0 - y / ColorBar.ActualHeight * 360.0);
                    ChangeColorByHSV(hsv, null);
                    break;
            }
        }

        static byte RoundToEvenByte(double value)
        {
            value = Math.Min(Math.Max(value, 0.0), 255.0);
            var t = (byte)value;
            if (value - t >= 0.5)
            {
                t++;
            }
            return t;
        }

        private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColorImage();
            SetSelectedPosition();
        }

        private void ColorPickArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsClickColorBar)
            {
                IsClickPickerArea = true;
                ColorPickArea.CaptureMouse();
                var point = e.GetPosition((IInputElement)sender);
                SelectColorFromPickerArea(point.X, point.Y);
            }
        }

        private void ColorPickArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClickPickerArea)
            {
                var point = e.GetPosition((IInputElement)sender);
                SelectColorFromPickerArea(point.X, point.Y);
            }
        }

        private void ColorPickArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClickPickerArea)
            {
                IsClickPickerArea = false;
                ColorPickArea.ReleaseMouseCapture();
                var point = e.GetPosition((IInputElement)sender);
                SelectColorFromPickerArea(point.X, point.Y);
            }
        }

        private void ColorBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsClickPickerArea)
            {
                IsClickColorBar = true;
                ColorBar.CaptureMouse();
                SelectColorFromColorBar(e.GetPosition((IInputElement)sender).Y);
            }
        }

        private void ColorBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClickColorBar)
            {
                SelectColorFromColorBar(e.GetPosition((IInputElement)sender).Y);
            }
        }

        private void ColorBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClickColorBar)
            {
                IsClickColorBar = false;
                ColorBar.ReleaseMouseCapture();
                SelectColorFromColorBar(e.GetPosition((IInputElement)sender).Y);
            }
        }

        private void ColorCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsValueChanging)
            {
                ChangeColor(ColorExtensions.FromHex(ColorCode.Text), sender);
            }
        }

        static void HSV_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker colorPicker)
            {
                return;
            }

            if (!colorPicker.IsValueChanging)
            {
                colorPicker.ChangeColorByHSV(new Hsv((float)colorPicker.Hue, (float)(colorPicker.Saturation * 0.01), (float)(colorPicker.Value * 0.01)), e.Property);
            }
        }

        static void RGB_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker colorPicker)
            {
                return;
            }

            if (!colorPicker.IsValueChanging)
            {
                colorPicker.ChangeColor(Color.FromRgb((byte)colorPicker.Red, (byte)colorPicker.Green, (byte)colorPicker.Blue), e.Property);
            }
        }

        static void OldColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker colorPicker)
            {
                return;
            }

            colorPicker.OldColorPreview.Fill = new SolidColorBrush(colorPicker.OldColor);
        }

        static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            const float ColorDiffEpsilon = 1.0F / 255.0F;

            if (d is not ColorPicker colorPicker)
            {
                return;
            }

            var newColor = colorPicker.Color;
            colorPicker.NewColorPreview.Fill = new SolidColorBrush(newColor);

            var newVectorColor = new Vector4(newColor.B, newColor.G, newColor.R, newColor.A) / 255.0F;
            var vectorDiff = Vector4.Abs(newVectorColor - colorPicker.VectorColor);
            if (vectorDiff.X >= ColorDiffEpsilon || vectorDiff.Y >= ColorDiffEpsilon || vectorDiff.Z >= ColorDiffEpsilon || vectorDiff.W >= ColorDiffEpsilon)
            {
                colorPicker.VectorColor = newVectorColor;
            }

            if (!colorPicker.IsValueChanging && e.OldValue is Color oldColor && newColor != oldColor)
            {
                colorPicker.ChangeColor(newColor, null);
            }
        }

        static void VectorColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker colorPicker)
            {
                return;
            }

            var newColor = colorPicker.VectorColor.ToColor();
            if (colorPicker.Color != newColor)
            {
                colorPicker.Color = newColor;
            }
        }

        static void ColorPickTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPicker)sender).UpdateColorImage();
            ((ColorPicker)sender).SetSelectedPosition();
        }
    }

    public enum ColorPickType
    {
        H,
        S,
        V,
        R,
        G,
        B
    }
}
