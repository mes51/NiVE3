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
using System.Windows.Shapes;
using NiVE3.UI.Internal;
using NiVE3.UI.Resources;
using NiVE3.Shared.Extension;
using NiVE3.UI.Command;
using System.Numerics;

namespace NiVE3.UI.Dialog
{
    /// <summary>
    /// ColorPickerDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPickerDialog : Window
    {
        public static readonly DependencyProperty OKButtonTextProperty = DependencyProperty.Register(
            nameof(OKButtonText),
            typeof(string),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata("OK")
        );

        public static readonly DependencyProperty CancelButtonTextProperty = DependencyProperty.Register(
            nameof(CancelButtonText),
            typeof(string),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata("キャンセル")
        );

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(Colors.Black, ColorChanged)
        );

        public static readonly DependencyProperty VectorColorProperty = DependencyProperty.Register(
            nameof(VectorColor),
            typeof(Vector4),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(Vector4.UnitW, VectorColorChanged)
        );

        internal static readonly DependencyProperty ColorPickTypeProperty = DependencyProperty.Register(
            nameof(ColorPickType),
            typeof(ColorPickType),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(ColorPickType.H, ColorPickTypeChanged)
        );

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

        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        public string OKButtonText
        {
            get { return (string)GetValue(OKButtonTextProperty); }
            set { SetValue(OKButtonTextProperty, value); }
        }

        public WriteableBitmap ColorPickAreaImage { get; } = new WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null);

        public WriteableBitmap ColorBarImage { get; } = new WriteableBitmap(24, 256, 96.0, 96.0, PixelFormats.Bgra32, null);

        bool IsClickPickerArea { get; set; }

        bool IsClickColorBar { get; set; }

        bool IsValueChanging { get; set; }

        byte[] ColorPickAreaImageData { get; } = Enumerable.Repeat<byte>(255, 256 * 256 * 4).ToArray();

        byte[] ColorBarImageData { get; } = Enumerable.Repeat<byte>(255, 24 * 256 * 4).ToArray();

        HSV HSV { get; set; } = new HSV(Colors.White);

        public ICommand OKCommand { get; }

        public ColorPickerDialog(Color oldColor)
        {
            OKCommand = new ActionCommand(() =>
            {
                DialogResult = true;
                Close();
            });

            IsValueChanging = true;
            InitializeComponent();
            var oldColorBrush = new SolidColorBrush(oldColor);
            oldColorBrush.Freeze();
            OldColor.Fill = oldColorBrush;
            IsValueChanging = false;
            ChangeColor(oldColor, null);
        }

        void UpdateColorImage()
        {
            if (ColorPickArea.ActualWidth < 1.0 || ColorPickArea.ActualHeight < 1.0 || ColorBar.ActualHeight < 1.0)
            {
                return;
            }

            Func<int, int, Color> areaFunc;
            Func<int, Color> barFunc;
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    {
                        var addX = 360.0 / ColorPickArea.ActualWidth;
                        var addY = 1.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) =>
                        {
                            var hsv = HSV.Copy();
                            hsv.H = addX * x;
                            hsv.V = 1.0 - addY * y;
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            var hsv = HSV.Copy();
                            hsv.S = 1.0 - (y / ColorBar.ActualHeight);
                            return hsv.ToRgb();
                        };
                    }
                    break;
                case ColorPickType.V:
                    {
                        var addX = 360.0 / ColorPickArea.ActualWidth;
                        var addY = 1.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) =>
                        {
                            var hsv = HSV.Copy();
                            hsv.H = addX * x;
                            hsv.S = 1.0 - addY * y;
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            var hsv = HSV.Copy();
                            hsv.V = 1.0 - (y / ColorBar.ActualHeight);
                            return hsv.ToRgb();
                        };
                    }
                    break;
                case ColorPickType.R:
                    {
                        var addX = 255.0 / ColorPickArea.ActualWidth;
                        var addY = 255.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) => Color.FromRgb(Color.R, (byte)Math.Round(255.0 - addY * y), (byte)Math.Round(addX * x));
                        barFunc = y => Color.FromRgb((byte)Math.Round(255.0 - (y / ColorBar.ActualHeight) * 255.0), Color.G, Color.B);
                    }
                    break;
                case ColorPickType.G:
                    {
                        var addX = 255.0 / ColorPickArea.ActualWidth;
                        var addY = 255.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) => Color.FromRgb((byte)Math.Round(255.0 - addY * y), Color.G, (byte)Math.Round(addX * x));
                        barFunc = y => Color.FromRgb(Color.R, (byte)Math.Round(255.0 - (y / ColorBar.ActualHeight) * 255.0), Color.B);
                    }
                    break;
                case ColorPickType.B:
                    {
                        var addX = 255.0 / ColorPickArea.ActualWidth;
                        var addY = 255.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) => Color.FromRgb((byte)Math.Round(addX * x), (byte)Math.Round(255.0 - addY * y), Color.B);
                        barFunc = y => Color.FromRgb(Color.R, Color.G, (byte)Math.Round(255.0 - (y / ColorBar.ActualHeight) * 255.0));
                    }
                    break;
                default:
                    {
                        var addX = 1.0 / ColorPickArea.ActualWidth;
                        var addY = 1.0 / ColorPickArea.ActualHeight;
                        areaFunc = (x, y) =>
                        {
                            var hsv = HSV.Copy();
                            hsv.S = addX * x;
                            hsv.V = 1.0 - addY * y;
                            return hsv.ToRgb();
                        };
                        barFunc = y =>
                        {
                            var hsv = new HSV(Colors.Red);
                            hsv.H = 360.0 - (y / ColorBar.ActualHeight) * 360.0;
                            return hsv.ToRgb();
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
                    var color = areaFunc(x, y);
                    ColorPickAreaImageData[pos] = color.B;
                    ColorPickAreaImageData[pos + 1] = color.G;
                    ColorPickAreaImageData[pos + 2] = color.R;
                }
            }

            for (int y = 0, pos = 0; y < barRect.Height; y++)
            {
                var color = barFunc(y);
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
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    circleX = HSV.H / 360.0 * ColorPickArea.ActualWidth;
                    circleY = (1.0 - HSV.V) * ColorPickArea.ActualHeight;
                    barY = (1.0 - HSV.S) * ColorBar.ActualHeight;
                    break;
                case ColorPickType.V:
                    circleX = HSV.H / 360.0 * ColorPickArea.ActualWidth;
                    circleY = (1.0 - HSV.S) * ColorPickArea.ActualHeight;
                    barY = (1.0 - HSV.V) * ColorBar.ActualHeight;
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
                    circleX = HSV.S * ColorPickArea.ActualWidth;
                    circleY = (1.0 - HSV.V) * ColorPickArea.ActualHeight;
                    barY = (360.0 - HSV.H) / 360.0 * ColorBar.ActualHeight;
                    break;
            }
            Canvas.SetLeft(SelectedColorCircle, circleX - 6);
            Canvas.SetTop(SelectedColorCircle, circleY - 6);
            Canvas.SetTop(SelectedColorBarArrow, barY - 2);
        }

        void ChangeColor(Color color, object? inputSource)
        {
            IsValueChanging = true;

            Color = color;
            HSV = new HSV(color);
            if (inputSource != HValue)
            {
                HValue.Value = HSV.H;
            }
            if (inputSource != SValue)
            {
                SValue.Value = HSV.S * 100.0;
            }
            if (inputSource != VValue)
            {
                VValue.Value = HSV.V * 100.0;
            }
            if (inputSource != RValue)
            {
                RValue.Value = color.R;
            }
            if (inputSource != GValue)
            {
                GValue.Value = color.G;
            }
            if (inputSource != BValue)
            {
                BValue.Value = color.B;
            }
            if (inputSource != ColorCode)
            {
                ColorCode.Text = color.ToHex();
            }

            IsValueChanging = false;

            UpdateColorImage();
            SetSelectedPosition();
        }

        void ChangeColorByHSV(HSV hsv, object? inputSource)
        {
            IsValueChanging = true;

            Color = HSV.ToRgb();
            HSV = hsv;
            if (inputSource != HValue)
            {
                HValue.Value = HSV.H;
            }
            if (inputSource != SValue)
            {
                SValue.Value = HSV.S * 100.0;
            }
            if (inputSource != VValue)
            {
                VValue.Value = HSV.V * 100.0;
            }
            if (inputSource != RValue)
            {
                RValue.Value = Color.R;
            }
            if (inputSource != GValue)
            {
                GValue.Value = Color.G;
            }
            if (inputSource != BValue)
            {
                BValue.Value = Color.B;
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
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    HSV.H = x / ColorPickArea.ActualWidth * 360.0;
                    HSV.V = 1.0 - y / ColorPickArea.ActualHeight;
                    ChangeColorByHSV(HSV, null);
                    break;
                case ColorPickType.V:
                    HSV.H = x / ColorPickArea.ActualWidth * 360.0;
                    HSV.S = 1.0 - y / ColorPickArea.ActualHeight;
                    ChangeColorByHSV(HSV, null);
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
                    HSV.S = x / ColorPickArea.ActualWidth;
                    HSV.V = 1.0 - y / ColorPickArea.ActualHeight;
                    ChangeColorByHSV(HSV, null);
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
            switch (ColorPickType)
            {
                case ColorPickType.S:
                    HSV.S = 1.0 - y / ColorBar.ActualHeight;
                    ChangeColorByHSV(HSV, null);
                    break;
                case ColorPickType.V:
                    HSV.V = 1.0 - y / ColorBar.ActualHeight;
                    ChangeColorByHSV(HSV, null);
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
                    HSV.H = 360.0 - y / ColorBar.ActualHeight * 360.0;
                    ChangeColorByHSV(HSV, null);
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

        private void RootWindow_ContentRendered(object sender, EventArgs e)
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

        private void HSV_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (!IsValueChanging)
            {
                HSV.H = HValue.Value;
                HSV.S = SValue.Value * 0.01;
                HSV.V = VValue.Value * 0.01;
                ChangeColorByHSV(HSV, sender);
            }
        }

        private void RGB_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (!IsValueChanging)
            {
                ChangeColor(Color.FromRgb((byte)RValue.Value, (byte)GValue.Value, (byte)BValue.Value), sender);
            }
        }

        private void ColorCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsValueChanging)
            {
                ChangeColor(ColorExtensions.FromHex(ColorCode.Text), sender);
            }
        }

        static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            const float ColorDiffEpsilon = 1.0F / 255.0F;

            if (d is not ColorPickerDialog dialog)
            {
                return;
            }

            var newColor = dialog.Color;
            dialog.NewColor.Fill = new SolidColorBrush(newColor);

            var newVectorColor = new Vector4(newColor.B, newColor.G, newColor.R, newColor.A) / 255.0F;
            var vectorDiff = Vector4.Abs(newVectorColor - dialog.VectorColor);
            if (vectorDiff.X >= ColorDiffEpsilon || vectorDiff.Y >= ColorDiffEpsilon || vectorDiff.Z >= ColorDiffEpsilon || vectorDiff.W >= ColorDiffEpsilon)
            {
                dialog.VectorColor = newVectorColor;
            }
        }

        static void VectorColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPickerDialog dialog)
            {
                return;
            }

            var newVectorColor = Vector4.Max(Vector4.Min(dialog.VectorColor, Vector4.One), Vector4.Zero) * 255.0F;
            var newColor = Color.FromArgb((byte)newVectorColor.W, (byte)newVectorColor.Z, (byte)newVectorColor.Y, (byte)newVectorColor.X);
            if (dialog.Color != newColor)
            {
                dialog.Color = newColor;
            }
        }

        static void ColorPickTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPickerDialog)sender).UpdateColorImage();
            ((ColorPickerDialog)sender).SetSelectedPosition();
        }
    }

    enum ColorPickType
    {
        H,
        S,
        V,
        R,
        G,
        B
    }
}
