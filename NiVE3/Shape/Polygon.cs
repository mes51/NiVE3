using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace NiVE3.Shape
{
    class Polygon
    {
        public Line[] Lines { get; }

        public float MinX { get; }

        public float MaxX { get; }

        public float MinY { get; }

        public float MaxY { get; }

        public Polygon(ReadOnlySpan<PointF> points)
        {
            var isOpen = points[^1].X != points[0].X || points[^1].Y != points[0].Y;
            Lines = new Line[points.Length - (isOpen ? 0 : 1)];
            for (var i = 1; i < points.Length; i++)
            {
                Lines[i - 1] = new Line(points[i - 1], points[i]);
            }
            if (isOpen)
            {
                Lines[^1] = new Line(points[^1], points[0]);
            }

            var min = Vector128.Create(float.MaxValue);
            var max = Vector128.Create(float.MinValue);
            var values = MemoryMarshal.Cast<PointF, float>(points);
            for (int i = 0; i < Lines.Length; i++)
            {
                var l = Lines[i];
                var v = Unsafe.As<Line, Vector4>(ref l).AsVector128();
                min = Sse.Min(min, v);
                max = Sse.Max(max, v);
            }

            min = Sse.Min(
                Sse.Shuffle(min, min, 0b01000100),
                Sse.Shuffle(min, min, 0b11101110)
            );
            max = Sse.Max(
                Sse.Shuffle(max, max, 0b01000100),
                Sse.Shuffle(max, max, 0b11101110)
            );

            MinX = min.GetElement(0);
            MinY = min.GetElement(1);
            MaxX = max.GetElement(0);
            MaxY = max.GetElement(1);
        }
    }
}
