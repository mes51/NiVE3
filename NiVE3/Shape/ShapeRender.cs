using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;

namespace NiVE3.Shape
{
    static class ShapeRender
    {
        public static void FillPolygonNonzero(Polygon[] polygons, NManagedImage image, Vector4 color, float offsetX = 0.0F, float offsetY = 0.0F)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            var minY = Math.Max((int)Math.Floor(polygons.Min(p => p.MinY) - offsetY), 0);
            var maxY = Math.Min((int)Math.Ceiling(polygons.Max(p => p.MaxY) - offsetY), image.Height);
            var minX = Math.Max((int)Math.Floor(polygons.Min(p => p.MinX) - offsetX), 0);
            var maxX = Math.Min((int)Math.Ceiling(polygons.Max(p => p.MaxX) - offsetX), image.Width);
            Parallel.For(minY, maxY, h =>
            {
                var width = image.Width;
                var data = MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan()).Slice(h * width, width);
                var temp = ArrayPool<float>.Shared.Rent(width);
                temp.AsSpan(0, width).Clear();

                for (var s = 0; s < 4; s++)
                {
                    var max = float.MinValue;
                    var hitLine = new List<Hit>();
                    for (var i = 0; i < polygons.Length; i++)
                    {
                        var y = h + 0.25F * s + offsetY;
                        if (polygons[i].MinY <= y && polygons[i].MaxY >= y)
                        {
                            var ls = polygons[i].Lines;
                            for (var n = 0; n < ls.Length; n++)
                            {
                                var p = ls[n].GetCrossHorizonalPositionAndDirection(y);
                                if (p.Value > float.MinValue)
                                {
                                    max = Math.Max(max, p.Value);
                                    hitLine.Add(p);
                                }
                            }
                        }
                    }
                    if (hitLine.Count > 0 && max > offsetX - 2.0F)
                    {
                        hitLine.Sort();
                        var hi = 0;
                        var hp = hitLine[0].Value;
                        var dir = false;
                        var depth = 0;
                        var inout = false;
                        var area = 0.0F;
                        for (var w = minX; w < maxX; w++)
                        {
                            var x = w + offsetX;
                            if (hp <= x)
                            {
                                var prev = inout;
                                if (depth > 0)
                                {
                                    if (dir != hitLine[hi].IsDown)
                                    {
                                        depth--;
                                    }
                                    else
                                    {
                                        depth++;
                                    }
                                }
                                else
                                {
                                    dir = hitLine[hi].IsDown;
                                    depth++;
                                }
                                if (hp < x && prev != depth > 0)
                                {
                                    if (prev)
                                    {
                                        area = 1.0F;
                                        area -= 1.0F - (hp - (int)hp);
                                    }
                                    else
                                    {
                                        area = 0.0F;
                                        area += 1.0F - (hp - (int)hp);
                                    }
                                    prev = depth > 0;
                                }
                                for (hi++; hi < hitLine.Count; hi++)
                                {
                                    hp = hitLine[hi].Value;
                                    var d = x - hp;
                                    if (hp > x)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (depth > 0)
                                        {
                                            if (dir != hitLine[hi].IsDown)
                                            {
                                                depth--;
                                            }
                                            else
                                            {
                                                depth++;
                                            }
                                        }
                                        else
                                        {
                                            dir = hitLine[hi].IsDown;
                                            depth++;
                                        }
                                        if (prev != depth > 0 && d < 1.0F && d > 0.0F)
                                        {
                                            if (prev)
                                            {
                                                area -= 1.0F - (hp - (int)hp);
                                            }
                                            else
                                            {
                                                area += 1.0F - (hp - (int)hp);
                                            }
                                            prev = depth > 0;
                                        }
                                    }
                                }
                                inout = depth > 0;
                            }

                            temp[w] += area;
                            if (inout)
                            {
                                area = 1.0F;
                            }
                            else
                            {
                                area = 0.0F;
                            }
                            if (hi >= hitLine.Count)
                            {
                                break;
                            }
                        }
                    }
                }
                for (var w = 0; w < width; w++)
                {
                    if (temp[w] > 0.0F)
                    {
                        data[w] = Blend.Process(BlendMode.Normal, data[w], color * new Vector4(1.0F, 1.0F, 1.0F, temp[w] * 0.25F));
                    }
                }

                ArrayPool<float>.Shared.Return(temp);
            });
        }

        public static void FillPolygonNonzeroAiliased(Polygon[] polygons, NManagedImage image, Vector4 color, float offsetX = 0.0F, float offsetY = 0.0F)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            var minY = Math.Max((int)Math.Floor(polygons.Min(p => p.MinY) - offsetY), 0);
            var maxY = Math.Min((int)Math.Ceiling(polygons.Max(p => p.MaxY) - offsetY), image.Height);
            var minX = Math.Max((int)Math.Floor(polygons.Min(p => p.MinX) - offsetX), 0);
            var maxX = Math.Min((int)Math.Ceiling(polygons.Max(p => p.MaxX) - offsetX), image.Width);
            Parallel.For(minY, maxY, h =>
            {
                var width = image.Width;
                var data = MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan()).Slice(h * width, width);

                var max = float.MinValue;
                var hitLine = new List<Hit>();
                for (var i = 0; i < polygons.Length; i++)
                {
                    var y = h + offsetY;
                    if (polygons[i].MinY <= y && polygons[i].MaxY >= y)
                    {
                        var ls = polygons[i].Lines;
                        for (var n = 0; n < ls.Length; n++)
                        {
                            var p = ls[n].GetCrossHorizonalPositionAndDirection(y);
                            if (p.Value > float.MinValue)
                            {
                                max = Math.Max(max, p.Value);
                                hitLine.Add(p);
                            }
                        }
                    }
                }
                if (hitLine.Count > 0 && max > offsetX - 2.0F)
                {
                    hitLine.Sort();
                    var hi = 0;
                    var hp = hitLine[0].Value;
                    var dir = false;
                    var depth = 0;
                    var inout = false;
                    for (var w = minX; w < maxX; w++)
                    {
                        var x = w + offsetX;
                        if (hp <= x)
                        {
                            var prev = inout;
                            if (depth > 0)
                            {
                                if (dir != hitLine[hi].IsDown)
                                {
                                    depth--;
                                }
                                else
                                {
                                    depth++;
                                }
                            }
                            else
                            {
                                dir = hitLine[hi].IsDown;
                                depth++;
                            }
                            if (hp < x && prev != depth > 0)
                            {
                                prev = depth > 0;
                            }
                            for (hi++; hi < hitLine.Count; hi++)
                            {
                                hp = hitLine[hi].Value;
                                var d = x - hp;
                                if (hp > x)
                                {
                                    break;
                                }
                                else
                                {
                                    if (depth > 0)
                                    {
                                        if (dir != hitLine[hi].IsDown)
                                        {
                                            depth--;
                                        }
                                        else
                                        {
                                            depth++;
                                        }
                                    }
                                    else
                                    {
                                        dir = hitLine[hi].IsDown;
                                        depth++;
                                    }
                                    if (prev != depth > 0 && d < 1.0F && d > 0.0F)
                                    {
                                        prev = depth > 0;
                                    }
                                }
                            }
                            inout = depth > 0;
                        }

                        if (inout)
                        {
                            data[w] = Blend.Process(BlendMode.Normal, data[w], color);
                        }
                        if (hi >= hitLine.Count)
                        {
                            break;
                        }
                    }
                }
            });
        }
    }
}
