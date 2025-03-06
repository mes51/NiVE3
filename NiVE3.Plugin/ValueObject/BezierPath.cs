using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    public class BezierPath
    {
        public Vector2d BeginPoint { get; }

        public ImmutableArray<BeziePoint> Points { get; }

        public bool IsClosed { get; }

        public BezierPath(Vector2d beginPoint, IEnumerable<BeziePoint> points, bool isClosed)
        {
            BeginPoint = beginPoint;
            Points = [..points];
            IsClosed = isClosed;
        }

        public BezierPath(Vector2d beginPoint, BeziePoint[] points, bool isClosed)
        {
            BeginPoint = beginPoint;
            Points = ImmutableArray.Create(points);
            IsClosed = isClosed;
        }
    }

    public record BeziePoint(Vector2d ControlPoint1, Vector2d ControlPoint2, Vector2d EndPoint, bool IsLinear);
}
