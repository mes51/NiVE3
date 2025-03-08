using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Acornima;
using NiVE3.Numerics;
using NiVE3.Plugin.ValueObject;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.Shape
{
    class BezierEllipsePolygon : IPath, ISimplePath
    {
        // SEE: https://cat-in-136.github.io/2014/03/bezier-1-kappa.html
        static readonly float Kappa = 0.5522847498307936F; // (4.0 * (Math.Sqrt(2.0) - 1) / 3.0);

        public bool IsClosed => true;

        public ReadOnlyMemory<PointF> Points
        {
            get
            {
                if (FlattenedPoints == null)
                {
                    var flattenedPointMemory = NativeSegment.Flatten();
                    FlattenedPoints = new PointF[flattenedPointMemory.Length];
                    flattenedPointMemory.CopyTo(FlattenedPoints);
                }

                return FlattenedPoints;
            }
        }

        public PathTypes PathType => PathTypes.Closed;

        public RectangleF Bounds { get; }

        public BezierPath BezierPath { get; }

        public CubicBezierLineSegment NativeSegment { get; }

        PointF[]? FlattenedPoints { get; set; }

        public BezierEllipsePolygon(float x, float y, float width, float height) : this(new Vector2(x, y), new Vector2(width, height)) { }

        public BezierEllipsePolygon(Vector2 position, Vector2 size)
        {
            var halfSize = size * 0.5F;
            var controlPointOffset = halfSize * Kappa;
            var leftTop = position - halfSize;
            var rightBottom = position + halfSize;

            Bounds = new RectangleF(leftTop, new SizeF(size.X, size.Y));

            BezierPath = new BezierPath(
                new Vector2d(position.X, leftTop.Y),
                [
                    new BeziePoint(new Vector2d(position.X + controlPointOffset.X, leftTop.Y), new Vector2d(rightBottom.X, position.Y - controlPointOffset.Y), new Vector2d(rightBottom.X, position.Y), false),
                    new BeziePoint(new Vector2d(rightBottom.X, position.Y + controlPointOffset.Y), new Vector2d(position.X + controlPointOffset.X, rightBottom.Y), new Vector2d(position.X, rightBottom.Y), false),
                    new BeziePoint(new Vector2d(position.X - controlPointOffset.X, rightBottom.Y), new Vector2d(leftTop.X, position.Y + controlPointOffset.Y), new Vector2d(leftTop.X, position.Y), false),
                    new BeziePoint(new Vector2d(leftTop.X, position.Y - controlPointOffset.Y), new Vector2d(position.X - controlPointOffset.X, leftTop.Y), new Vector2d(position.X, leftTop.Y), false)
                ],
                true
            );

            NativeSegment = new CubicBezierLineSegment([
                new PointF(position.X, leftTop.Y),
                new PointF(position.X + controlPointOffset.X, leftTop.Y),
                new PointF(rightBottom.X, position.Y - controlPointOffset.Y),

                new PointF(rightBottom.X, position.Y),
                new PointF(rightBottom.X, position.Y + controlPointOffset.Y),
                new PointF(position.X + controlPointOffset.X, rightBottom.Y),

                new PointF(position.X, rightBottom.Y),
                new PointF(position.X - controlPointOffset.X, rightBottom.Y),
                new PointF(leftTop.X, position.Y + controlPointOffset.Y),

                new PointF(leftTop.X, position.Y),
                new PointF(leftTop.X, position.Y - controlPointOffset.Y),
                new PointF(position.X - controlPointOffset.X, leftTop.Y),

                new PointF(position.X, leftTop.Y)
            ]);
        }

        private BezierEllipsePolygon(BezierEllipsePolygon basePolygon, Matrix3x2 transform)
        {
            NativeSegment = basePolygon.NativeSegment.Transform(transform);

            var controlPoints = NativeSegment.ControlPoints;
            BezierPath = new BezierPath(
                (Vector2d)(Vector2)controlPoints[0],
                controlPoints.Skip(1).Chunk(3).Select(p => new BeziePoint((Vector2d)(Vector2)p[0], (Vector2d)(Vector2)p[1], (Vector2d)(Vector2)p[2], false)),
                true
            );

            var baseBounds = basePolygon.Bounds;
            var p1 = Vector2.Transform(new Vector2(baseBounds.Left, baseBounds.Top), transform);
            var p2 = Vector2.Transform(new Vector2(baseBounds.Right, baseBounds.Top), transform);
            var p3 = Vector2.Transform(new Vector2(baseBounds.Right, baseBounds.Bottom), transform);
            var p4 = Vector2.Transform(new Vector2(baseBounds.Left, baseBounds.Bottom), transform);

            var minX = Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X);
            var maxX = Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X);
            var minY = Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y);
            var maxY = Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y);

            Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        public IPath AsClosedPath()
        {
            return this;
        }

        public IEnumerable<ISimplePath> Flatten()
        {
            return [this];
        }

        public IPath Transform(Matrix3x2 matrix)
        {
            return new BezierEllipsePolygon(this, matrix);
        }
    }
}
