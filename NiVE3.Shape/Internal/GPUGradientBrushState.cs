using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Numerics;

namespace NiVE3.Shape.Internal
{
    readonly struct GPULinearGradientBrush
    {
        public readonly Float2 Begin;

        public readonly Bool UseOKLabInterpolation;

        public readonly float Length;

        public readonly Bool Reversed;

        public readonly Float2 SinCos;

        public readonly float Opacity;

        public GPULinearGradientBrush(bool useOKLabInterpolation, float opacity, Vector2 begin, Vector2 end)
        {
            UseOKLabInterpolation = useOKLabInterpolation;
            Opacity = opacity;
            Begin = begin;

            // 逆算のためXが先
            var rad = Math.Atan2(end.X - Begin.X, end.Y - Begin.Y);
            var matrix = Matrix3x3.Identity
                .Translate(-Begin.X, -Begin.Y)
                .Rotate((float)(rad / Math.PI * 180.0));
            var tsy = matrix.Transform(begin).Y;
            var tey = matrix.Transform(end).Y;

            if (tsy > tey)
            {
                Reversed = true;
            }
            Length = Math.Max(Reversed ? tsy - tey : tey - tsy, float.Epsilon);
            SinCos = new float2((float)Math.Sin(rad), (float)Math.Cos(rad));
        }
    }

    readonly struct GPURadialGradientBrush
    {
        public readonly Float2 Begin;

        public readonly Bool UseOKLabInterpolation;

        public readonly float Length;

        public readonly float Opacity;

        public GPURadialGradientBrush(bool useOKLabInterpolation, float opacity, Vector2 begin, Vector2 end)
        {
            UseOKLabInterpolation = useOKLabInterpolation;
            Opacity = opacity;
            Begin = begin;
            Length = Vector2.Distance(begin, end);
        }
    }
}
