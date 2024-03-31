using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
        ColorStop[] ColorStops { get; }

        Vector2 Start { get; }

        float Length { get; }

        bool Reversed { get; }

        float Sin { get; }

        float Cos { get; }

        public LinearGradientBrush(ColorStop[] colorStops, Vector2 start, Vector2 end)
        {
            if (colorStops.Length < 1)
            {
                throw new ArgumentException(null, nameof(colorStops));
            }

            ColorStops = colorStops;
            Start = start;

            // 逆算のためXが先
            var rad = Math.Atan2(end.X - start.X, end.Y - start.Y);
            var matrix = Matrix3x3.Identity
                .Translate(-start.X, -start.Y)
                .Rotate((float)(rad / Math.PI * 180.0));
            var tsy = matrix.Transform(start).Y;
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
            var p = Sin * (x - Start.X) + Cos * (y - Start.Y);
            if (Reversed)
            {
                p = Length - p;
            }
            var pos = (Reversed ? Length - p : p) / Length;

            var startStopIndex = ColorStops.IndexOf(c => c.Pos <= pos);
            if (startStopIndex < -1)
            {
                return ColorStops.First().Color;
            }
            var endStopIndex = ColorStops.IndexOf(c => c.Pos > pos);
            if (endStopIndex < 0)
            {
                return ColorStops.Last().Color;
            }

            var startColorStop = ColorStops[startStopIndex];
            var endColorStop = ColorStops[endStopIndex];
            return Vector4.Lerp(startColorStop.Color, endColorStop.Color, (pos - startColorStop.Pos) / (endColorStop.Pos - startColorStop.Pos));
        }
    }

    class RadialGradientBrush : Brush
    {
        ColorStop[] ColorStops { get; }

        Vector2 Start { get; }

        float Length { get; }

        public RadialGradientBrush(ColorStop[] colorStops, Vector2 start, Vector2 end)
        {
            if (colorStops.Length < 1)
            {
                throw new ArgumentException(null, nameof(colorStops));
            }

            ColorStops = colorStops;
            Start = start;
            Length = Vector2.Distance(start, end);
        }

        public override Vector4 GetColor(float x, float y)
        {
            var pos = Vector2.Distance(new Vector2(x, y), Start) / Length;

            var startStopIndex = ColorStops.IndexOf(c => c.Pos <= pos);
            if (startStopIndex < -1)
            {
                return ColorStops.First().Color;
            }
            var endStopIndex = ColorStops.IndexOf(c => c.Pos > pos);
            if (endStopIndex < 0)
            {
                return ColorStops.Last().Color;
            }

            var startColorStop = ColorStops[startStopIndex];
            var endColorStop = ColorStops[endStopIndex];
            return Vector4.Lerp(startColorStop.Color, endColorStop.Color, (pos - startColorStop.Pos) / (endColorStop.Pos - startColorStop.Pos));
        }
    }

    readonly record struct ColorStop(Vector4 Color, float Pos) { }
}
