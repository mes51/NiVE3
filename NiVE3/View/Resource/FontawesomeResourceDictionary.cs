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

        [ShowInMarkup, Icon("")]
        public static readonly string Eye = nameof(Eye);

        [ShowInMarkup, Icon("")]
        public static readonly string VolumeHigh = nameof(VolumeHigh);

        [ShowInMarkup, Icon("")]
        public static readonly string Circle = nameof(Circle);

        [ShowInMarkup, Icon("")]
        public static readonly string Lock = nameof(Lock);

        [ShowInMarkup, Icon("")]
        public static readonly string Tag = nameof(Tag);

        [ShowInMarkup, Icon("")]
        public static readonly string Sun = nameof(Sun);

        [ShowInMarkup, Icon("")]
        public static readonly string FlorinSign = nameof(FlorinSign);

        [ShowInMarkup, Icon("")]
        public static readonly string CircleHalfStroke = nameof(CircleHalfStroke);

        [ShowInMarkup, Icon("")]
        public static readonly string Cube = nameof(Cube);

        [ShowInMarkup, Icon("")]
        public static readonly string Mound = nameof(Mound);

        [ShowInMarkup, Icon("")]
        public static readonly string MountainSun = nameof(MountainSun);

        [ShowInMarkup, Icon("")]
        public static readonly string LeftLong = nameof(LeftLong);

        [ShowInMarkup, Icon("")]
        public static readonly string RightLong = nameof(RightLong);

        [ShowInMarkup, Icon("")]
        public static readonly string Stopwatch = nameof(Stopwatch);

        [ShowInMarkup, Icon("")]
        public static readonly string Spinner = nameof(Spinner);

        [ShowInMarkup, Icon("")]
        public static readonly string Bold = nameof(Bold);

        [ShowInMarkup, Icon("")]
        public static readonly string Italic = nameof(Italic);

        [ShowInMarkup, Icon("")]
        public static readonly string AlignLeft = nameof(AlignLeft);

        [ShowInMarkup, Icon("")]
        public static readonly string AlignCenter = nameof(AlignCenter);

        [ShowInMarkup, Icon("")]
        public static readonly string AlignRight = nameof(AlignRight);

        [ShowInMarkup, Icon("+", Size = 8)]
        public static readonly string SmallPlus = nameof(SmallPlus);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string Hand = nameof(Hand);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string Cursor = nameof(Cursor);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string RotateRight = nameof(RotateRight);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string ArrowsUpDownLeftRight = nameof(ArrowsUpDownLeftRight);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string NoteSticky = nameof(NoteSticky);

        [ShowInMarkup, Icon("", Size = 12)]
        public static readonly string TrackCan = nameof(TrackCan);

        public FontawesomeResourceDictionary()
        {
            var keys = typeof(FontawesomeResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => ((string)f.GetValue(null)!, f.GetCustomAttribute<IconAttribute>()));
            var fontFamily = new FontFamily(new Uri("pack://application:,,,/NiVE3;component/Resources/Font Awesome 6 Free-Solid-900.otf"), "#Font Awesome 6 Free Solid");
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var dip = VisualTreeHelper.GetDpi(new FrameworkElement()).PixelsPerDip;
            foreach (var (key, attr) in keys)
            {
                if (attr == null)
                {
                    continue;
                }

                var size = attr.Size;
                var ft = new FormattedText(attr.Glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, attr.Size, Brushes.Black, dip);
                if (size < ft.Width)
                {
                        ft = new FormattedText(attr.Glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size * (size / ft.Width), Brushes.Black, dip);
                }
                this[key] = ft.BuildGeometry(new Point());
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class IconAttribute : Attribute
        {
            public string Glyph { get; }

            public double Size { get; set; } = 12.0;

            public IconAttribute(string glyph)
            {
                Glyph = glyph;
            }
        }
    }
}
