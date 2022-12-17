using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Extension
{
    static class ColorExtensions
    {
        public static Color FromHex(string hex)
        {
            var colorCode = hex.StartsWith("#") ? hex.Substring(1) : hex;
            var colors = colorCode.Grouped(2).Select(c => (byte)Convert.ToInt32(new string(c.ToArray()), 16)).ToArray();
            if (colors.Length > 3)
            {
                return Color.FromArgb(
                    colors.FirstOrDefault(),
                    colors.Skip(1).FirstOrDefault(),
                    colors.Skip(2).FirstOrDefault(),
                    colors.Skip(3).FirstOrDefault()
                );
            }
            else
            {
                return Color.FromRgb(
                    colors.FirstOrDefault(),
                    colors.Skip(1).FirstOrDefault(),
                    colors.Skip(2).FirstOrDefault()
                );
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
