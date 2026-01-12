using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.ValueObject;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.Extension
{
    static class BezierPathExtensions
    {
        public static IPath? BuildPath(this BezierPath? path)
        {
            if (path == null || path.IsEmpty())
            {
                return null;
            }

            var pathBuilder = new PathBuilder();
            pathBuilder.StartFigure();
            pathBuilder.MoveTo((Vector2)path.BeginPoint.EndPoint);
            var lastPoint = path.BeginPoint;
            foreach (var p in path.Points)
            {
                if (p.IsLinear && lastPoint.IsLinear)
                {
                    pathBuilder.LineTo((Vector2)p.EndPoint);
                }
                else
                {
                    var nextControlPoint = (Vector2)(lastPoint.EndPoint + (lastPoint.IsLinear ? Vector2d.Zero : lastPoint.NextControlPoint));
                    var prevControlPoint = (Vector2)(p.EndPoint + (p.IsLinear ? Vector2d.Zero : p.PrevControlPoint));
                    pathBuilder.CubicBezierTo(nextControlPoint, prevControlPoint, (Vector2)p.EndPoint);
                }
                lastPoint = p;
            }

            if (path.IsClosed)
            {
                if (!path.BeginPoint.IsLinear || !lastPoint.IsLinear)
                {
                    var nextControlPoint = (Vector2)(lastPoint.EndPoint + (lastPoint.IsLinear ? Vector2d.Zero : lastPoint.NextControlPoint));
                    var prevControlPoint = (Vector2)(path.BeginPoint.EndPoint + (path.BeginPoint.IsLinear ? Vector2d.Zero : path.BeginPoint.PrevControlPoint));
                    pathBuilder.CubicBezierTo(nextControlPoint, prevControlPoint, (Vector2)path.BeginPoint.EndPoint);
                }
                pathBuilder.CloseFigure();
            }

            return pathBuilder.Build();
        }
    }
}
