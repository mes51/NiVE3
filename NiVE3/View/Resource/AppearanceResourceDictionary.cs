using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary(IsPublic = true)]
    public class AppearanceResourceDictionary : ResourceDictionary
    {
        public static readonly AppearanceResourceDictionary Dictionary;

        static Dictionary<string, AppearanceChangeableAttribute> ColorKeys { get; }

        [ShowInMarkup, BrushColorRange("#7F9C90", "#7F9C90")]
        public static readonly string AudioLevelMeter1DisableFill = nameof(AudioLevelMeter1DisableFill);

        [ShowInMarkup, BrushColorRange("#939654", "#939654")]
        public static readonly string AudioLevelMeter2DisableFill = nameof(AudioLevelMeter2DisableFill);

        [ShowInMarkup, BrushColorRange("#99755E", "#99755E")]
        public static readonly string AudioLevelMeter3DisableFill = nameof(AudioLevelMeter3DisableFill);

        [ShowInMarkup, BrushColorRange("#954245", "#954245")]
        public static readonly string AudioLevelMeter4DisableFill = nameof(AudioLevelMeter4DisableFill);

        [ShowInMarkup, BrushColorRange("#953033", "#953033")]
        public static readonly string AudioLevelMeterOverZeroDisableFill = nameof(AudioLevelMeterOverZeroDisableFill);

        [ShowInMarkup, BrushColorRange("#CDFCE9", "#CDFCE9")]
        public static readonly string AudioLevelMeter1EnableFill = nameof(AudioLevelMeter1EnableFill);

        [ShowInMarkup, BrushColorRange("#EDF288", "#EDF288")]
        public static readonly string AudioLevelMeter2EnableFill = nameof(AudioLevelMeter2EnableFill);

        [ShowInMarkup, BrushColorRange("#F7BC97", "#F7BC97")]
        public static readonly string AudioLevelMeter3EnableFill = nameof(AudioLevelMeter3EnableFill);

        [ShowInMarkup, BrushColorRange("#F16A6F", "#F16A6F")]
        public static readonly string AudioLevelMeter4EnableFill = nameof(AudioLevelMeter4EnableFill);

        [ShowInMarkup, BrushColorRange("#F14D53", "#F14D53")]
        public static readonly string AudioLevelMeterOverZeroEnableFill = nameof(AudioLevelMeterOverZeroEnableFill);

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

            Dictionary = new AppearanceResourceDictionary();
        }

        public AppearanceResourceDictionary()
        {
            appearance = 1.0;
            Update();
        }

        public Brush GetBrush(string key)
        {
            return (this[key] as Brush) ?? Brushes.Transparent;
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
            var colorCode = hex.StartsWith("#") ? hex.Substring(1) : hex;
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
