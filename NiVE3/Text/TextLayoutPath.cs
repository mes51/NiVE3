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
        bool NotRotateCharacter { get; }

        float BeginOffsetPosition { get; }

        bool IsClosed { get; }

        (Vector2 first, Vector2 second, float rad, float length)[] TextPathPoints { get; }

        float TotalLength { get; }

        public TextLayoutPath(ISimplePath path, bool isInvert, bool notRotateCharacter, double beginOffset)
        {
            NotRotateCharacter = notRotateCharacter;
            IsClosed = path.IsClosed;

            var points = path.Points.ToArray();
            if (points.Length < 2)
            {
                TextPathPoints = [];
                return;
            }

            var radianOffset = 0.0F;
            if (isInvert)
            {
                Array.Reverse(points);
                radianOffset = MathF.PI;
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
                var diff = sv - fv;
                var rad = MathF.Atan2(diff.Y, diff.X) - radianOffset;
                return (fv, sv, rad, (sv - fv).Length());
            })];

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
                    var (first, second, rad, length) = TextPathPoints[0];
                    var pos = Vector2.Lerp(first, second, x / length);
                    var translate = Matrix3x2.CreateTranslation(pos - location);
                    if (NotRotateCharacter)
                    {
                        return translate;
                    }
                    else
                    {
                        return translate * Matrix3x2.CreateRotation(rad, pos);
                    }
                }
                else if (x >= TotalLength)
                {
                    var (first, second, rad, length) = TextPathPoints[^1];
                    var pos = Vector2.Lerp(first, second, (x - TotalLength) / length + 1.0F);
                    var translate = Matrix3x2.CreateTranslation(pos - location);
                    if (NotRotateCharacter)
                    {
                        return translate;
                    }
                    else
                    {
                        return translate * Matrix3x2.CreateRotation(rad, pos);
                    }
                }
            }
            if (x < 0.0F)
            {
                x = (x % TotalLength) + TotalLength;
            }
            while (true)
            {
                foreach (var (first, second, rad, length) in TextPathPoints)
                {
                    if (x > length)
                    {
                        x -= length;
                        continue;
                    }

                    var pos = Vector2.Lerp(first, second, x / length);
                    var translate = Matrix3x2.CreateTranslation(pos - location);
                    if (NotRotateCharacter)
                    {
                        return translate;
                    }
                    else
                    {
                        return translate * Matrix3x2.CreateRotation(rad, pos);
                    }
                }
            }
        }
    }
}
