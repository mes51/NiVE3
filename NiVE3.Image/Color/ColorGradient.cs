using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Color;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    /// <summary>
    /// グラデーションを表します
    /// </summary>
    /// <param name="ColorStops">色のグラデーションの分岐点</param>
    /// <param name="OpacityStops">不透明度の分岐点</param>
    public record ColorGradient(IReadOnlyList<ColorStop> ColorStops, IReadOnlyList<OpacityStop> OpacityStops)
    {
        public static readonly ColorGradient Empty = new ColorGradient([], []);

        public static readonly ColorGradient WhiteBlackGradient = new ColorGradient([new ColorStop(Vector4.One, 0.0F), new ColorStop(Vector4.UnitW, 1.0F)], [new OpacityStop(1.0F, 0.0F), new OpacityStop(1.0F, 1.0F)]);

        /// <summary>
        /// グラデーションを表します
        /// </summary>
        /// <param name="colorStops">色のグラデーションの分岐点</param>
        /// <param name="opacityStops">不透明度の分岐点</param>
        public ColorGradient(IEnumerable<ColorStop> colorStops, IEnumerable<OpacityStop> opacityStops) : this([..colorStops], [..opacityStops]) { }

        /// <summary>
        /// 指定した位置の色を取得します
        /// </summary>
        /// <param name="position">0～1の位置</param>
        /// <param name="useLabInterpolation">色の補間にOkLab色空間を使用するかどうか</param>
        /// <returns>補完された色</returns>
        public Vector4 GetrColor(float position, bool useLabInterpolation = false)
        {
            var color = GetRgb(position, useLabInterpolation);
            color.W = GetOpacity(position);

            return color;
        }

        public object Serialize()
        {
            return new Dictionary<string, object?>
            {
                { nameof(ColorStops), ColorStops.Select(c => c.Serialize()).ToArray() },
                { nameof(OpacityStops), OpacityStops.Select(o => o.Serialize()).ToArray() }
            };
        }

        public static ColorGradient? Deserialize(object serializedValue)
        {
            if (serializedValue is IDictionary<string, object> dic &&
                dic.TryGetValue(nameof(ColorStops), out var colorStops) &&
                dic.TryGetValue(nameof(OpacityStops), out var opacityStops) &&
                colorStops is object[] serializedColorStops &&
                opacityStops is object[] serializedOpacityStops)
            {
                return new ColorGradient([..serializedColorStops.Select(ColorStop.Deserialize).NonNull()], [..serializedOpacityStops.Select(OpacityStop.Deserialize).NonNull()]);
            }
            else
            {
                return null;
            }
        }

        Vector4 GetRgb(float position, bool useLabInterpolation = false)
        {
            if (ColorStops.Count < 1)
            {
                return Vector4.One;
            }
            else if (ColorStops.Count < 2)
            {
                return ColorStops[0].Color;
            }

            var p = ColorStops.LastOrDefault(c => c.Position <= position, ColorStops.First());
            var n = ColorStops.FirstOrDefault(c => c.Position > position, ColorStops.Last());
            if (p == n)
            {
                return p.Color;
            }

            if (useLabInterpolation)
            {
                ref var pLab = ref Unsafe.As<OkLab, Vector4>(ref p.OkLabColor);
                ref var nLab = ref Unsafe.As<OkLab, Vector4>(ref n.OkLabColor);
                var resultLab = Vector4.Lerp(pLab, nLab, (position - p.Position) / (n.Position - p.Position));
                return Unsafe.As<Vector4, OkLab>(ref resultLab).ToRgb();
            }
            else
            {
                return Vector4.Lerp(p.Color, n.Color, (position - p.Position) / (n.Position - p.Position));
            }
        }

        float GetOpacity(float position)
        {
            if (OpacityStops.Count < 1)
            {
                return 1.0F;
            }
            else if (OpacityStops.Count < 2)
            {
                return OpacityStops[0].Opacity;
            }

            var p = OpacityStops.LastOrDefault(o => o.Position <= position, OpacityStops.First());
            var n = OpacityStops.FirstOrDefault(o => o.Position > position, OpacityStops.Last());

            if (p == n)
            {
                return p.Opacity;
            }
            else
            {
                return float.Lerp(p.Opacity, n.Opacity, (position - p.Position) / (n.Position - p.Position));
            }
        }
    }

    /// <summary>
    /// 色のグラデーションの分岐点を表します
    /// </summary>
    /// <param name="Color">色</param>
    /// <param name="Position">0～1の位置</param>
    public record ColorStop(Vector4 Color, float Position)
    {
        /// <summary>
        /// この分岐点のOkLab色空間での色
        /// </summary>
        public OkLab OkLabColor = OkLab.FromRgb(Color);

        /// <summary>
        /// Jsonにシリアライズ可能な形式に変換します
        /// </summary>
        /// <returns>Jsonに変換可能な値</returns>
        public object Serialize()
        {
            return new SerializedColorStop(Color.Z, Color.Y, Color.X, Color.W, Position);
        }

        /// <summary>
        /// Jsonからデシリアライズします
        /// </summary>
        /// <param name="serializedValue">Jsonから取得した値</param>
        /// <returns>デシリアライズされたColorStop、デシリアライズ出来なかった場合はnull</returns>
        public static ColorStop? Deserialize(object serializedValue)
        {
            if (serializedValue is IDictionary<string, object> dic &&
                dic.TryGetValue(nameof(SerializedColorStop.R), out var r) &&
                dic.TryGetValue(nameof(SerializedColorStop.G), out var g) &&
                dic.TryGetValue(nameof(SerializedColorStop.B), out var b) &&
                dic.TryGetValue(nameof(SerializedColorStop.A), out var a) &&
                dic.TryGetValue(nameof(SerializedColorStop.Position), out var position))
            {
                return new ColorStop(
                    new Vector4(
                        Convert.ToSingle(b),
                        Convert.ToSingle(g),
                        Convert.ToSingle(r),
                        Convert.ToSingle(a)
                    ),
                    Convert.ToSingle(position)
                );
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 不透明度のグラデーションの分岐点を表します
    /// </summary>
    /// <param name="Opacity">不透明度</param>
    /// <param name="Position">0～1の位置</param>
    public record OpacityStop(float Opacity, float Position)
    {
        /// <summary>
        /// Jsonにシリアライズ可能な形式に変換します
        /// </summary>
        /// <returns>Jsonに変換可能な値</returns>
        public object Serialize()
        {
            return this;
        }

        /// <summary>
        /// Jsonからデシリアライズします
        /// </summary>
        /// <param name="serializedValue">Jsonから取得した値</param>
        /// <returns>デシリアライズされたOpacityStop、デシリアライズ出来なかった場合はnull</returns>
        public static OpacityStop? Deserialize(object serializedValue)
        {
            if (serializedValue is IDictionary<string, object> dic &&
                dic.TryGetValue(nameof(OpacityStop.Opacity), out var opacity) &&
                dic.TryGetValue(nameof(OpacityStop.Position), out var position))
            {
                return new OpacityStop(Convert.ToSingle(opacity), Convert.ToSingle(position));
            }
            else
            {
                return null;
            }
        }
    }

    file record SerializedColorStop(float R, float G, float B, float A, float Position);
}
