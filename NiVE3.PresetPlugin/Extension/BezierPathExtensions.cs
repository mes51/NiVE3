using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.PresetPlugin.Extension
{
    static class BezierPathExtensions
    {
        public static IPath? BuildPath(this BezierPath path)
        {
            if (path.IsEmpty())
            {
                return null;
            }

            var pathBuilder = new PathBuilder();
            pathBuilder.StartFigure();
            pathBuilder.MoveTo((Vector2)path.BeginPoint.EndPoint);
            var lastPoint = path.BeginPoint;
            foreach (var p in path.Points)
            {
                if (p.IsLinear)
                {
                    pathBuilder.LineTo((Vector2)p.EndPoint);
                }
                else
                {
                    pathBuilder.CubicBezierTo((Vector2)(lastPoint.NextControlPoint + lastPoint.EndPoint), (Vector2)(p.PrevControlPoint + p.EndPoint), (Vector2)p.EndPoint);
                }
                lastPoint = p;
            }

            if (path.IsClosed)
            {
                if (!path.BeginPoint.IsLinear || !lastPoint.IsLinear)
                {
                    pathBuilder.CubicBezierTo((Vector2)(lastPoint.NextControlPoint + lastPoint.EndPoint), (Vector2)(path.BeginPoint.PrevControlPoint + path.BeginPoint.EndPoint), (Vector2)path.BeginPoint.EndPoint);
                }
                pathBuilder.CloseFigure();
            }

            return pathBuilder.Build();
        }
    }
}
