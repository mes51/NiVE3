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

        public abstract void Transform(Matrix3x3 matrix);
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

        public override void Transform(Matrix3x3 matrix) { }
    }

    class LinearGradientBrush : Brush
    {
        ColorGradient ColorGradient { get; }

        bool UseOkLabInterpolation { get; }

        Vector2 Begin { get; set; }

        Vector2 End { get; set; }

        float Length { get; set; }

        bool Reversed { get; set; }

        float Sin { get; set; }

        float Cos { get; set; }

        public LinearGradientBrush(ColorGradient colorGradient, bool useOkLabInterpolation, Vector2 begin, Vector2 end)
        {
            ColorGradient = colorGradient;
            UseOkLabInterpolation = useOkLabInterpolation;
            Begin = begin;
            End = end;

            UpdateGradientParams();
        }

        public override Vector4 GetColor(float x, float y)
        {
            var p = Sin * (x - Begin.X) + Cos * (y - Begin.Y);
            if (Reversed)
            {
                p = Length - p;
            }
            var pos = (Reversed ? Length - p : p) / Length;
            return ColorGradient.GetrColor(pos, UseOkLabInterpolation);
        }

        public override void Transform(Matrix3x3 matrix)
        {
            Begin = matrix.Transform(Begin);
            End = matrix.Transform(End);
            UpdateGradientParams();
        }

        void UpdateGradientParams()
        {
            // 逆算のためXが先
            var rad = Math.Atan2(End.X - Begin.X, End.Y - Begin.Y);
            var matrix = Matrix3x3.Identity
                .Translate(-Begin.X, -Begin.Y)
                .Rotate((float)(rad / Math.PI * 180.0));
            var tsy = matrix.Transform(Begin).Y;
            var tey = matrix.Transform(End).Y;

            if (tsy > tey)
            {
                Reversed = true;
            }
            Length = Math.Max(Reversed ? tsy - tey : tey - tsy, float.Epsilon);
            Sin = (float)Math.Sin(rad);
            Cos = (float)Math.Cos(rad);
        }
    }

    class RadialGradientBrush : Brush
    {
        ColorGradient ColorGradient { get; }

        bool UseOkLabInterpolation { get; }

        Vector2 Begin { get; set; }

        Vector2 End { get; set; }

        float Length { get; set; }

        public RadialGradientBrush(ColorGradient colorGradient, bool useOkLabInterpolation, Vector2 begin, Vector2 end)
        {
            ColorGradient = colorGradient;
            UseOkLabInterpolation = useOkLabInterpolation;
            Begin = begin;
            End = end;
            Length = Vector2.Distance(begin, end);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var pos = Vector2.Distance(new Vector2(x, y), Begin) / Length;
            return ColorGradient.GetrColor(pos, UseOkLabInterpolation);
        }

        public override void Transform(Matrix3x3 matrix)
        {
            Begin = matrix.Transform(Begin);
            End = matrix.Transform(End);
            Length = Vector2.Distance(Begin, End);
        }
    }
}
