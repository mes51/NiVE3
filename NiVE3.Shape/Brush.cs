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
    public abstract class Brush
    {
        public abstract bool IsVisible();

        public abstract Vector4 GetColor(float x, float y);

        public abstract void Transform(Matrix3x3 matrix);
    }

    public class SolidBrush : Brush
    {
        public Vector4 Color { get; }

        public SolidBrush(Vector4 color)
        {
            Color = color;
        }

        public override bool IsVisible()
        {
            return Color.W > 0.0F;
        }

        public override Vector4 GetColor(float x, float y)
        {
            return Color;
        }

        public override void Transform(Matrix3x3 matrix) { }
    }

    public abstract class GradientBrush : Brush
    {
        public ColorGradient ColorGradient { get; }

        public float Opacity { get; }

        public bool UseOkLabInterpolation { get; }

        protected GradientBrush(ColorGradient colorGradient, float opacity, bool useOkLabInterpolation)
        {
            ColorGradient = colorGradient;
            Opacity = opacity;
            UseOkLabInterpolation = useOkLabInterpolation;
        }
    }

    public class LinearGradientBrush : GradientBrush
    {
        public Vector2 Begin { get; private set; }

        public Vector2 End { get; private set; }

        float Length { get; set; }

        bool Reversed { get; set; }

        float Sin { get; set; }

        float Cos { get; set; }

        public LinearGradientBrush(ColorGradient colorGradient, float opacity, bool useOkLabInterpolation, Vector2 begin, Vector2 end)
            : base(colorGradient, opacity, useOkLabInterpolation)
        {
            Begin = begin;
            End = end;

            UpdateGradientParams();
        }

        public override bool IsVisible()
        {
            return ColorGradient.OpacityStops.Any(o => o.Opacity > 0.0F);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var p = Sin * (x - Begin.X) + Cos * (y - Begin.Y);
            var pos = (Reversed ? Length - p : p) / Length;
            var color = ColorGradient.GetrColor(pos, UseOkLabInterpolation);
            color.W *= Opacity;
            return color;
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

    public class RadialGradientBrush : GradientBrush
    {
        public Vector2 Begin { get; private set; }

        public Vector2 End { get; private set; }

        float Length { get; set; }

        public RadialGradientBrush(ColorGradient colorGradient, float opacity, bool useOkLabInterpolation, Vector2 begin, Vector2 end)
            : base(colorGradient, opacity, useOkLabInterpolation)
        {
            Begin = begin;
            End = end;
            Length = Vector2.Distance(begin, end);
        }

        public override bool IsVisible()
        {
            return ColorGradient.OpacityStops.Any(o => o.Opacity > 0.0F);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var pos = Vector2.Distance(new Vector2(x, y), Begin) / Length;
            var color = ColorGradient.GetrColor(pos, UseOkLabInterpolation);
            color.W *= Opacity;
            return color;
        }

        public override void Transform(Matrix3x3 matrix)
        {
            Begin = matrix.Transform(Begin);
            End = matrix.Transform(End);
            Length = Vector2.Distance(Begin, End);
        }
    }
}
