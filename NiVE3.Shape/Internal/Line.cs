using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace NiVE3.Shape.Internal
{
    readonly struct Line
    {
        public Line(PointF start, PointF end)
        {
            StartX = start.X;
            StartY = start.Y;
            EndX = end.X;
            EndY = end.Y;
            DX = end.X - start.X;
            DY = end.Y - start.Y;
            IsDown = DY >= 0.0F;
        }

        public readonly float StartX;

        public readonly float StartY;

        public readonly float EndX;

        public readonly float EndY;

        public readonly bool IsDown;

        readonly float DX = 0.0F;

        readonly float DY = 0.0F;

        public Hit GetCrossHorizonalPositionAndDirection(float y)
        {
            var sy = StartY - y;
            var ey = EndY - y;
            if (sy > 0.0F == ey <= 0.0F)
            {
                return new Hit(-sy / DY * DX + StartX, IsDown);
            }
            else
            {
                return Hit.Empty;
            }
        }
    }

    readonly struct Hit : IComparable<Hit>
    {
        public static readonly Hit Empty = new Hit(float.MinValue, false);

        public readonly float Value;

        public readonly bool IsDown;

        public Hit(float value, bool isDown)
        {
            Value = value;
            IsDown = isDown;
        }

        public int CompareTo(Hit other)
        {
            if (Value > other.Value)
            {
                return 1;
            }
            else if (Value < other.Value)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
