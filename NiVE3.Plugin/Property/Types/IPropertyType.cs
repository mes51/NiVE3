using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

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
        /// エクスプレッションをサポートするかどうか
        /// </summary>
        bool IsSupportedExpression { get; }

        /// <summary>
        /// グラフエディタでの表示をサポートするかどうか
        /// </summary>
        bool IsSupportedGraphEditor { get; }

        /// <summary>
        /// 2つのプロパティを補間します
        /// </summary>
        /// <param name="keyFrames">2つ以上の時間順でソートされたキーフレーム</param>
        /// <param name="t">現在時刻</param>
        /// <returns>補間された値</returns>
        object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time);

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

        /// <summary>
        /// 画像のキャッシュの際に使用するハッシュ計算用の値に変換します
        /// </summary>
        /// <param name="value">プロパティの値</param>
        /// <returns>ハッシュ計算用のbyteのSpan</returns>
        Span<byte> ConvertToHashBase(object? value);

        /// <summary>
        /// エクスプレッションで処理された後の値から、このプロパティの型の値に変換します
        /// </summary>
        /// <param name="expressionValue">エクスプレッションから返ってきた値。プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</param>
        /// <param name="rawValue">エクスプレッション適用前の値</param>
        /// <param name="value">このプロパティの型の値。変換できなかった場合は不定</param>
        /// <returns>このプロパティの型の値に変換できたかどうか</returns>
        bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value);

        /// <summary>
        /// このプロパティの型の値からエクスプレッションで使用可能な値に変換します
        /// </summary>
        /// <param name="value">このプロパティの型の値</param>
        /// <returns>プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</returns>
        object? ConvertToExpressionValue(object? value);

        /// <summary>
        /// グラフエディタで表示する際の値を取得します
        /// </summary>
        /// <param name="keyFrames">2つ以上の時間順でソートされたキーフレーム</param>
        /// <param name="t">取得する時間</param>
        /// <returns>数値化されたプロパティの値</returns>
        double GetGraphValue(IReadOnlyList<KeyFrame> keyFrames, Time time) => 0.0;
    }

    public interface ILayerDependPropertyType : IPropertyType
    {
        /// <summary>
        /// エクスプレッションで処理された後の値から、このプロパティの型の値に変換します
        /// </summary>
        /// <param name="expressionValue">エクスプレッションから返ってきた値。プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</param>
        /// <param name="rawValue">エクスプレッション適用前の値</param>
        /// <param name="layer">レイヤー</param>
        /// <param name="value">このプロパティの型の値。変換できなかった場合は不定</param>
        /// <returns>このプロパティの型の値に変換できたかどうか</returns>
        bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, ILayerObject layer, out object? value);

        /// <summary>
        /// このプロパティの型の値からエクスプレッションで使用可能な値に変換します
        /// </summary>
        /// <param name="value">このプロパティの型の値</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</returns>
        object? ConvertToExpressionValue(object? value, ILayerObject layer);
    }

    public interface ICompositionDependIPropertyType : IPropertyType
    {
        /// <summary>
        /// エクスプレッションで処理された後の値から、このプロパティの型の値に変換します
        /// </summary>
        /// <param name="expressionValue">エクスプレッションから返ってきた値。プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</param>
        /// <param name="rawValue">エクスプレッション適用前の値</param>
        /// <param name="composition">コンポジション</param>
        /// <param name="value">このプロパティの型の値。変換できなかった場合は不定</param>
        /// <returns>このプロパティの型の値に変換できたかどうか</returns>
        bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, ICompositionObject composition, out object? value);

        /// <summary>
        /// このプロパティの型の値からエクスプレッションで使用可能な値に変換します
        /// </summary>
        /// <param name="value">このプロパティの型の値</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>プリミティブ型、またはstringの単体、配列、IDictionary&lt;string, object?&gt;のいずれか</returns>
        object? ConvertToExpressionValue(object? value, ICompositionObject composition);
    }
}
