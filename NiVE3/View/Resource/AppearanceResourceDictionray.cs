using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using NiVE3.Extension;
using NiVE3.Config;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary]
    class AppearanceResourceDictionary : ResourceDictionary
    {
        static Dictionary<string, AppearanceChangeableAttribute> ColorKeys { get; }

        [ShowInMarkup, BrushColorRange("#313131", "#FFFFFF")]
        public static readonly string BackgroundFill = nameof(BackgroundFill);

        [ShowInMarkup, BrushColorRange("#FFFFFF", "#000000")]
        public static readonly string BorderBrush = nameof(BorderBrush);

        [ShowInMarkup, BrushColorRange("#FFFFFF", "#313131")]
        public static readonly string TextBrush = nameof(TextBrush);

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
            appearance = ApplicationSetting.Setting.Appearance;
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
}
