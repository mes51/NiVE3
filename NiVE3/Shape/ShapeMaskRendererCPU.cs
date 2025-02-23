using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Drawing;
using NiVE3.Image;

namespace NiVE3.Shape
{
    // NOTE: マスクはNonZeroのみ
    static class ShapeMaskRendererCPU
    {
        const int SuperSamplingCount = 8;

        const float SamplingRate = 1.0F / SuperSamplingCount;

        public static void Fill(Polygon[] polygons, ManagedRasterizedMaskImage image, float opacity, float offsetX = 0.0F, float offsetY = 0.0F, MaskBlendMode blendMode = MaskBlendMode.Add)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            offsetX++;
            var minY = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinY) - offsetY), 0);
            var maxY = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxY) - offsetY), image.Height);
            var minX = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinX) - offsetX), 0) - 1;
            var maxX = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxX) - offsetX) + 1, image.Width);
            Parallel.For(minY, maxY, h =>
            {
                var width = image.Width;
                var data = image.GetDataSpan().Slice(h * width, width);
                var temp = ArrayPool<float>.Shared.Rent(width);
                temp.AsSpan(0, width).Clear();

                for (var s = 0; s < SuperSamplingCount; s++)
                {
                    var max = float.MinValue;
                    var hitLine = new List<Hit>();
                    for (var i = 0; i < polygons.Length; i++)
                    {
                        var y = h + SamplingRate * s + offsetY;
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
                                        area -= 1.0F - (hp - (int)Math.Floor(hp));
                                    }
                                    else
                                    {
                                        area = 0.0F;
                                        area += 1.0F - (hp - (int)Math.Floor(hp));
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
                                                area -= 1.0F - (hp - (int)Math.Floor(hp));
                                            }
                                            else
                                            {
                                                area += 1.0F - (hp - (int)Math.Floor(hp));
                                            }
                                            prev = depth > 0;
                                        }
                                    }
                                }
                                inout = depth > 0;
                            }

                            if (w > -1)
                            {
                                temp[w] += area;
                            }
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
                        data[w] = MaskBlend.Process(blendMode, data[w], opacity * temp[w] * SamplingRate);
                    }
                }

                ArrayPool<float>.Shared.Return(temp);
            });
        }

        public static void FillAiliased(Polygon[] polygons, ManagedRasterizedMaskImage image, float opacity, float offsetX = 0.0F, float offsetY = 0.0F, MaskBlendMode blendMode = MaskBlendMode.Add)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            var minY = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinY) - offsetY), 0);
            var maxY = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxY) - offsetY), image.Height);
            var minX = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinX) - offsetX), 0);
            var maxX = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxX) - offsetX), image.Width);
            Parallel.For(minY, maxY, h =>
            {
                var width = image.Width;
                var data = image.GetDataSpan().Slice(h * width, width);

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
                            data[w] = MaskBlend.Process(blendMode, data[w], opacity);
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
