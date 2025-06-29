using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.PresetPlugin.Effect.Util
{
    class LayoutPath
    {
        public float TotalLength { get; }

        public bool IsClosed { get; }

        bool NotRotate { get; }

        float BeginOffsetPosition { get; }

        (Vector2 first, Vector2 second, float rad, float length)[] PathPoints { get; }

        public LayoutPath(PathBuilder pathBuilder, bool isInvert, bool notRotate, double beginOffset) :
            this(pathBuilder.Build().Flatten().FirstOrDefault() ?? EmptySimplePath.Instance, isInvert, notRotate, beginOffset) { }

        public LayoutPath(ISimplePath path, bool isInvert, bool notRotate, double beginOffset)
        {
            NotRotate = notRotate;
            IsClosed = path.IsClosed;

            var points = path.Points.ToArray();
            if (points.Length < 2)
            {
                PathPoints = [];
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
            PathPoints = [..points.Zip(nextPoints, (f, s) =>
            {
                var fv = (Vector2)f;
                var sv = (Vector2)s;
                var diff = sv - fv;
                var rad = MathF.Atan2(diff.Y, diff.X) - radianOffset;
                return (fv, sv, rad, (sv - fv).Length());
            })];

            TotalLength = PathPoints.Aggregate(0.0F, (m, t) => t.length + m);
            BeginOffsetPosition = (float)(beginOffset * TotalLength * (isInvert ? -1.0 : 1.0));
        }

        public Vector2 AlignToPath(float x, Vector2 location)
        {
            if (PathPoints.Length < 1)
            {
                return location;
            }

            return Vector2.Transform(location, GetAlignToPathMatrix(x));
        }

        public Matrix3x2 GetAlignToPathMatrix(float x)
        {
            if (PathPoints.Length < 1)
            {
                return Matrix3x2.Identity;
            }

            x += BeginOffsetPosition;

            if (!IsClosed)
            {
                if (x < 0.0F)
                {
                    var (first, second, rad, length) = PathPoints[0];
                    var pos = Vector2.Lerp(first, second, x / length);
                    var translate = Matrix3x2.CreateTranslation(pos);
                    if (NotRotate)
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
                    var (first, second, rad, length) = PathPoints[^1];
                    var pos = Vector2.Lerp(first, second, (x - TotalLength) / length + 1.0F);
                    var translate = Matrix3x2.CreateTranslation(pos);
                    if (NotRotate)
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
                foreach (var (first, second, rad, length) in PathPoints)
                {
                    if (x > length)
                    {
                        x -= length;
                        continue;
                    }

                    var pos = Vector2.Lerp(first, second, x / length);
                    var translate = Matrix3x2.CreateTranslation(pos);
                    if (NotRotate)
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

    file class EmptySimplePath : ISimplePath
    {
        public static readonly EmptySimplePath Instance = new EmptySimplePath();

        public bool IsClosed => false;

        public ReadOnlyMemory<SixLabors.ImageSharp.PointF> Points => new ReadOnlyMemory<SixLabors.ImageSharp.PointF>([]);

        private EmptySimplePath() { }
    }
}
