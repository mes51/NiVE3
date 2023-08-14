using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary]
    class FontawesomeResourceDictionary : ResourceDictionary
    {
        [ShowInMarkup, Icon("")]
        public static readonly string FaceMehBlank = nameof(FaceMehBlank);

        [ShowInMarkup, Icon("")]
        public static readonly string Film = nameof(Film);

        public FontawesomeResourceDictionary()
        {
            var keys = typeof(FontawesomeResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => ((string)f.GetValue(null)!, f.GetCustomAttribute<IconAttribute>()));
            var fontFamily = new FontFamily(new Uri("pack://application:,,,/NiVE3;component/Resources/Font Awesome 6 Free-Solid-900.otf"), "#Font Awesome 6 Free Solid");
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            foreach (var (key, attr) in keys)
            {
                if (attr == null)
                {
                    continue;
                }

                var ft = new FormattedText(attr.Glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, attr.Size, Brushes.Black, 96.0);
                this[key] = ft.BuildGeometry(new Point());
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class IconAttribute : Attribute
        {
            public string Glyph { get; }

            public double Size { get; set; } = 16.0;

            public IconAttribute(string glyph)
            {
                Glyph = glyph;
            }
        }
    }
}
