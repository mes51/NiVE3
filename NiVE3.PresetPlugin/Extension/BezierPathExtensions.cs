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
            pathBuilder.MoveTo((Vector2)path.BeginPoint);
            foreach (var p in path.Points)
            {
                if (p.IsLinear)
                {
                    pathBuilder.LineTo((Vector2)p.EndPoint);
                }
                else
                {
                    pathBuilder.CubicBezierTo((Vector2)p.ControlPoint1, (Vector2)p.ControlPoint2, (Vector2)p.EndPoint);
                }
            }

            if (path.IsClosed)
            {
                pathBuilder.CloseFigure();
            }

            return pathBuilder.Build();
        }
    }
}
