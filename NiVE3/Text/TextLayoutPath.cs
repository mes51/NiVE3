using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.Text
{
    class TextLayoutPath
    {
        float BeginOffsetPosition { get; }

        bool IsClosed { get; }

        (Vector2 first, Vector2 second, float length)[] TextPathPoints { get; }

        float BeginRadian { get; }

        float EndRadian { get; }

        float TotalLength { get; }

        float RadianOffset { get; }

        public TextLayoutPath(ISimplePath path, bool isInvert, double beginOffset)
        {
            IsClosed = path.IsClosed;

            var points = path.Points.ToArray();
            if (points.Length < 2)
            {
                TextPathPoints = [];
                BeginRadian = 0.0F;
                EndRadian = 0.0F;
                return;
            }

            if (isInvert)
            {
                Array.Reverse(points);
                RadianOffset = 0.0F;
            }
            else
            {
                RadianOffset = MathF.PI;
            }

            var nextPoints = points.Skip(1);
            if (path.IsClosed)
            {
                nextPoints = nextPoints.Append(points[0]);
            }
            TextPathPoints = [..points.Zip(nextPoints, (f, s) =>
            {
                var fv = (Vector2)f;
                var sv = (Vector2)s;
                return (fv, sv, (sv - fv).Length());
            })];

            var diff = TextPathPoints[0].second - TextPathPoints[0].first;
            BeginRadian = MathF.Atan2(diff.Y, diff.X) - RadianOffset;

            diff = TextPathPoints[^1].second - TextPathPoints[^1].second;
            EndRadian = MathF.Atan2(diff.Y, diff.X) - RadianOffset;

            TotalLength = TextPathPoints.Aggregate(0.0F, (m, t) => t.length + m);
            BeginOffsetPosition = (float)(beginOffset * TotalLength);
        }

        public Matrix3x2 AlignToPath(float x, Vector2 location)
        {
            if (TextPathPoints.Length < 1)
            {
                return Matrix3x2.CreateTranslation(new Vector2(x, 0.0F) - location);
            }

            x += BeginOffsetPosition;

            if (!IsClosed)
            {
                if (x < 0.0F)
                {
                    var pos = Vector2.Lerp(TextPathPoints[0].first, TextPathPoints[0].second, x / TextPathPoints[0].length);
                    return Matrix3x2.CreateTranslation(pos - location) * Matrix3x2.CreateRotation(BeginRadian, pos);
                }
                else if (x >= TotalLength)
                {
                    var pos = Vector2.Lerp(TextPathPoints[^1].first, TextPathPoints[^1].second, (x - TotalLength) / TextPathPoints[^1].length);
                    return Matrix3x2.CreateTranslation(pos - location) * Matrix3x2.CreateRotation(EndRadian, pos);
                }
            }
            if (x < 0.0F)
            {
                x = (x % TotalLength) + TotalLength;
            }
            while (true)
            {
                foreach (var (first, second, length) in TextPathPoints)
                {
                    if (x > length)
                    {
                        x -= length;
                        continue;
                    }

                    var pos = Vector2.Lerp(first, second, x / length);
                    var diff = first - second;
                    var rad = MathF.Atan2(diff.Y, diff.X);

                    return Matrix3x2.CreateTranslation(pos - location) * Matrix3x2.CreateRotation(rad - RadianOffset, pos);
                }
            }
        }
    }
}
