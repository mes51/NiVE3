using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.UI.Resources
{
    [MarkupableResourceDictionary(IsPublic = true)]
    public class AppearanceResourceDictionary : ResourceDictionary
    {
        static Dictionary<string, AppearanceChangeableAttribute> ColorKeys { get; }

        [ShowInMarkup, BrushColorRange("#313131", "#FFFFFF")]
        public static readonly string BackgroundFill = nameof(BackgroundFill);

        [ShowInMarkup, BrushColorRange("#3399FF", "#3399FF")]
        public static readonly string SelectedBackgroundFill = nameof(SelectedBackgroundFill);

        [ShowInMarkup, BrushColorRange("#FFFFFF", "#000000")]
        public static readonly string BorderBrush = nameof(BorderBrush);

        [ShowInMarkup, BrushColorRange("#4488FF", "#88AAFF")]
        public static readonly string MouseOverBorderBrush = nameof(MouseOverBorderBrush);

        [ShowInMarkup, BrushColorRange("#88AAFF", "#4488FF")]
        public static readonly string FocusedBorderBrush = nameof(FocusedBorderBrush);

        [ShowInMarkup, BrushColorRange("#FFFFFF", "#313131")]
        public static readonly string TextBrush = nameof(TextBrush);

        [ShowInMarkup, BrushColorRange("#999999", "#BBBBBB")]
        public static readonly string DisableTextBrush = nameof(DisableTextBrush);

        [ShowInMarkup, BrushColorRange("#0000FF", "#3355FF")]
        public static readonly string LinkTextBrush = nameof(LinkTextBrush);

        [ShowInMarkup, BrushColorRange("#3355FF", "#6699FF")]
        public static readonly string MouseOverLinkTextBrush = nameof(MouseOverLinkTextBrush);

        [ShowInMarkup, BrushColorRange("#553366FF", "#2288EEFF")]
        public static readonly string TimeIndicatorFrameRangeBrush = nameof(TimeIndicatorFrameRangeBrush);

        [ShowInMarkup, BrushColorRange("#11FFFFFF", "#11000000")]
        public static readonly string OutOfWorkareaBrush = nameof(OutOfWorkareaBrush);

        double appearance = 0.0;
        public double Appearance
        {
            get => appearance;
            set
            {
                if (value != appearance)
                {
                    appearance = value;
                    Update();
                }
            }
        }

        static AppearanceResourceDictionary()
        {
            ColorKeys = typeof(AppearanceResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => ((string)f.GetValue(null)!, f.GetCustomAttribute<AppearanceChangeableAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Item1, t => t.Item2!);
        }

        public AppearanceResourceDictionary()
        {
            appearance = 1.0;
            Update();
        }

        void Update()
        {
            foreach (var (key, attribute) in ColorKeys)
            {
                this[key] = attribute.GetValue(Appearance);
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        abstract class AppearanceChangeableAttribute : Attribute
        {
            public abstract object GetValue(double appearance);
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class BrushColorRangeAttribute : AppearanceChangeableAttribute
        {
            public Color DarkColor { get; set; }

            public Color LightColor { get; set; }

            public BrushColorRangeAttribute(string darkColorHex, string lightColorHex)
            {
                DarkColor = ColorExtensions.FromHex(darkColorHex);
                LightColor = ColorExtensions.FromHex(lightColorHex);
            }

            public override object GetValue(double appearance)
            {
                var brush = new SolidColorBrush(DarkColor.Interpolate(LightColor, appearance));
                brush.Freeze();
                return brush;
            }
        }
    }

    file static class ColorExtensions
    {
        public static Color FromHex(string hex)
        {
            var colorCode = hex.StartsWith('#') ? hex[1..] : hex;
            var colors = new List<byte>();
            for (var i = 0; i < colorCode.Length; i += 2)
            {
                colors.Add((byte)Convert.ToInt32(colorCode.Substring(i, 2), 16));
            }
            if (colors.Count > 3)
            {
                return Color.FromArgb(colors[0], colors[1], colors[2], colors[3]);
            }
            else
            {
                return Color.FromRgb(colors[0], colors[1], colors[2]);
            }
        }

        public static string ToHex(this Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        public static Color Interpolate(this Color source, Color next, double t)
        {
            var a = (byte)Math.Round(source.A + (next.A - source.A) * t);
            var r = (byte)Math.Round(source.R + (next.R - source.R) * t);
            var g = (byte)Math.Round(source.G + (next.G - source.G) * t);
            var b = (byte)Math.Round(source.B + (next.B - source.B) * t);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
