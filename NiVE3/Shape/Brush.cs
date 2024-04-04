using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Color;
using NiVE3.Numerics;
using NiVE3.Shared.Extension;

namespace NiVE3.Shape
{
    abstract class Brush
    {
        public abstract Vector4 GetColor(float x, float y);
    }

    class SolidBrush : Brush
    {
        public Vector4 Color { get; }

        public SolidBrush(Vector4 color)
        {
            Color = color;
        }

        public override Vector4 GetColor(float x, float y)
        {
            return Color;
        }
    }

    class LinearGradientBrush : Brush
    {
        ColorGradient ColorGradient { get; }

        Vector2 Begin { get; }

        float Length { get; }

        bool Reversed { get; }

        float Sin { get; }

        float Cos { get; }

        public LinearGradientBrush(ColorGradient colorGradient, Vector2 begin, Vector2 end)
        {
            ColorGradient = colorGradient;
            Begin = begin;

            // 逆算のためXが先
            var rad = Math.Atan2(end.X - begin.X, end.Y - begin.Y);
            var matrix = Matrix3x3.Identity
                .Translate(-begin.X, -begin.Y)
                .Rotate((float)(rad / Math.PI * 180.0));
            var tsy = matrix.Transform(begin).Y;
            var tey = matrix.Transform(end).Y;

            if (tsy > tey)
            {
                Reversed = true;
            }
            Length = Math.Max(Reversed ? tsy - tey : tey - tsy, float.Epsilon);
            Sin = (float)Math.Sin(rad);
            Cos = (float)Math.Cos(rad);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var p = Sin * (x - Begin.X) + Cos * (y - Begin.Y);
            if (Reversed)
            {
                p = Length - p;
            }
            var pos = (Reversed ? Length - p : p) / Length;
            return ColorGradient.GetrColor(pos);
        }
    }

    class RadialGradientBrush : Brush
    {
        ColorGradient ColorGradient { get; }

        Vector2 Begin { get; }

        float Length { get; }

        public RadialGradientBrush(ColorGradient colorGradient, Vector2 begin, Vector2 end)
        {
            ColorGradient = colorGradient;
            Begin = begin;
            Length = Vector2.Distance(begin, end);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var pos = Vector2.Distance(new Vector2(x, y), Begin) / Length;
            return ColorGradient.GetrColor(pos);
        }
    }
}
