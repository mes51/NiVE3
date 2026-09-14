using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// トランスフォーム適用後の四角形 (任意の回転・スケール・射影に対応)。
    /// 頂点はローカル矩形の 左上→右上→右下→左下 に対応する。
    /// </summary>
    readonly record struct TextQuad(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
    {
        /// <summary>
        /// スクリーン空間での AABB を返す
        /// </summary>
        public Rect GetBounds()
        {
            var minX = Math.Min(Math.Min(TopLeft.X, TopRight.X), Math.Min(BottomRight.X, BottomLeft.X));
            var maxX = Math.Max(Math.Max(TopLeft.X, TopRight.X), Math.Max(BottomRight.X, BottomLeft.X));
            var minY = Math.Min(Math.Min(TopLeft.Y, TopRight.Y), Math.Min(BottomRight.Y, BottomLeft.Y));
            var maxY = Math.Max(Math.Max(TopLeft.Y, TopRight.Y), Math.Max(BottomRight.Y, BottomLeft.Y));
            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// 点が Quad の内側 (境界含む) にあるかを返す
        /// </summary>
        public bool Contains(Point point)
        {
            // 4 辺との外積の符号がすべて同じなら内側
            var cross0 = Cross(TopLeft, TopRight, point);
            var cross1 = Cross(TopRight, BottomRight, point);
            var cross2 = Cross(BottomRight, BottomLeft, point);
            var cross3 = Cross(BottomLeft, TopLeft, point);
            var allNonNegative = cross0 >= 0 && cross1 >= 0 && cross2 >= 0 && cross3 >= 0;
            var allNonPositive = cross0 <= 0 && cross1 <= 0 && cross2 <= 0 && cross3 <= 0;
            return allNonNegative || allNonPositive;
        }

        static double Cross(Point a, Point b, Point point)
        {
            return ((b.X - a.X) * (point.Y - a.Y)) - ((b.Y - a.Y) * (point.X - a.X));
        }
    }
}