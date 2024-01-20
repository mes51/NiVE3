using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property.Control;

namespace NiVE3.Plugin.Property.Types
{
    /// <summary>
    /// プロパティの型を表します
    /// </summary>
    public interface IPropertyType
    {
        /// <summary>
        /// 対応するプロパティの値の補間方法
        /// </summary>
        InterpolationType SupportedInterpolationTypes { get; }

        /// <summary>
        /// 2つのプロパティを補間します
        /// </summary>
        /// <param name="keyFrames">2つ以上の時間順でソートされたキーフレーム</param>
        /// <param name="t">現在時刻。0～1で表されます</param>
        /// <returns>補間された値</returns>
        object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t);

        /// <summary>
        /// 他のプロパティの値から現在のプロパティの値に変更します。
        /// </summary>
        /// <param name="otherValue">変換元のプロパティの値</param>
        /// <param name="convertedValue">変換後のプロパティの値</param>
        /// <returns>値が変換できた場合はtrue、非対応等で変換できなかった場合はfalse</returns>
        bool TryConvertFrom(object otherValue, out object convertedValue);

        /// <summary>
        /// 値をJSONで保存可能な形式にシリアライズします
        /// </summary>
        /// <param name="value">プロパティの値</param>
        /// <returns>シリアライズされた値</returns>
        object? SerializeValue(object? value);

        /// <summary>
        /// JSONに保存したをデシリアライズします
        /// </summary>
        /// <param name="serializedValue">シリアライズされた値</param>
        /// <returns>プロパティの値</returns>
        object? DeserializeValue(object? serializedValue);
    }
}
