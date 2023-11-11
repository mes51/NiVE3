using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.UI.Internal
{
    class HSV
    {
        const double ByteToDouble = 1.0 / 255.0;

        public HSV(Color c) : this(c.R, c.G, c.B) { }

        public HSV(byte r, byte g, byte b)
        {
            var tr = r * ByteToDouble;
            var tg = g * ByteToDouble;
            var tb = b * ByteToDouble;
            var max = Max(tr, tg, tb);
            var min = Min(tr, tg, tb);
            if (max > 0.0)
            {
                s = (max - min) / max;
            }
            else
            {
                s = 0.0;
            }
            v = max;
            if (max == min)
            {
                h = 0.0F;
            }
            else
            {
                if (r > g)
                {
                    if (r > b)
                    {
                        h = (tg - tb) / (max - min) * 60.0F;
                    }
                    else
                    {
                        h = (tr - tg) / (max - min) * 60.0F + 240.0F;
                    }
                }
                else if (g > b)
                {
                    h = (tb - tr) / (max - min) * 60.0F + 120.0F;
                }
                else
                {
                    h = (tr - tg) / (max - min) * 60.0F + 240.0F;
                }
                h = Rotate(h);
            }
        }

        HSV(double th, double ts, double tv)
        {
            h = th;
            s = ts;
            v = tv;
        }

        double h = 0.0F;
        double s = 0.0F;
        double v = 0.0F;
        public double H
        {
            get => h;
            set => h = value;
        }
        public double S
        {
            get => s;
            set
            {
                s = Math.Max(Math.Min(value, 1.0), 0.0);
            }
        }
        public double V
        {
            get => v;
            set
            {
                v = Math.Max(Math.Min(value, 1.0), 0.0);
            }
        }

        public Color ToRgb()
        {
            var th = Rotate(h) / 60.0F;
            var thi = ((int)th) % 6;
            var f = th - thi;
            var p = v * (1.0 - s);
            var q = v * (1.0 - f * s);
            var t = v * (1.0 - (1.0 - f) * s);
            switch (thi)
            {
                case 1:
                    {
                        return Color.FromRgb(RoundToEvenByte(q * 255.0), RoundToEvenByte(v * 255.0), RoundToEvenByte(p * 255.0));
                    }
                case 2:
                    {
                        return Color.FromRgb(RoundToEvenByte(p * 255.0), RoundToEvenByte(v * 255.0), RoundToEvenByte(t * 255.0));
                    }
                case 3:
                    {
                        return Color.FromRgb(RoundToEvenByte(p * 255.0), RoundToEvenByte(q * 255.0), RoundToEvenByte(v * 255.0));
                    }
                case 4:
                    {
                        return Color.FromRgb(RoundToEvenByte(t * 255.0), RoundToEvenByte(p * 255.0), RoundToEvenByte(v * 255.0));
                    }
                case 5:
                    {
                        return Color.FromRgb(RoundToEvenByte(v * 255.0), RoundToEvenByte(p * 255.0), RoundToEvenByte(q * 255.0));
                    }
                default:
                    {
                        return Color.FromRgb(RoundToEvenByte(v * 255.0), RoundToEvenByte(t * 255.0), RoundToEvenByte(p * 255.0));
                    }
            }
        }

        public HSV Copy()
        {
            return new HSV(h, s, v);
        }

        static double Min(double v1, double v2, double v3)
        {
            return Math.Min(Math.Min(v1, v2), v3);
        }

        static double Max(double v1, double v2, double v3)
        {
            return Math.Max(Math.Max(v1, v2), v3);
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

        static double Rotate(double value)
        {
            while (value >= 360.0)
            {
                value -= 360.0;
            }
            while (value < 0.0)
            {
                value += 360.0;
            }
            return value;
        }
    }
}
