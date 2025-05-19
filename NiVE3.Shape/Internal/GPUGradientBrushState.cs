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
        public readonly float2 Begin;

        public readonly Bool UseOKLabInterpolation;

        public readonly float Length;

        public readonly Bool Reversed;

        public readonly float2 SinCos;

        public GPULinearGradientBrush(bool useOKLabInterpolation, Vector2 begin, Vector2 end)
        {
            UseOKLabInterpolation = useOKLabInterpolation;
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
        public readonly float2 Begin;

        public readonly Bool UseOKLabInterpolation;

        public readonly float Length;

        public GPURadialGradientBrush(bool useOKLabInterpolation, Vector2 begin, Vector2 end)
        {
            UseOKLabInterpolation = useOKLabInterpolation;
            Begin = begin;
            Length = Vector2.Distance(begin, end);
        }
    }
}
