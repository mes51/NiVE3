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
        /// プロパティの次元の分割に対応するかどうか
        /// </summary>
        bool SupportSplitDimension => false;

        /// <summary>
        /// 次元の分割をした際の各次元のPropertyType
        /// </summary>
        IPropertyType? DimensionType => null;

        /// <summary>
        /// 次元の分割で分割された後のプロパティの数
        /// </summary>
        int DimensionCount => 0;

        /// <summary>
        /// 対応するプロパティの値の補間方法
        /// </summary>
        InterpolationType[] SupportedInterpolationTypes { get; }

        /// <summary>
        /// プロパティの編集用コントロールを生成します
        /// </summary>
        /// <returns>生成したPropertyControl</returns>
        PropertyControlBase CreateControl();

        /// <summary>
        /// プロパティの初期値を生成します
        /// </summary>
        /// <returns>プロパティの値の初期値</returns>
        object CreateValue();

        /// <summary>
        /// 2つのプロパティを補間します
        /// </summary>
        /// <param name="value1">1つ目のプロパティの値</param>
        /// <param name="value2">2つ目のプロパティの値</param>
        /// <param name="easeIn">1つ目のプロパティのEase</param>
        /// <param name="easeOut">2つ目のプロパティのEase</param>
        /// <param name="interpolationType">使用する補間方法</param>
        /// <param name="t">現在時刻。0～1で表されます</param>
        /// <returns>補間された値</returns>
        object Interpolate(object value1, object value2, Ease easeIn, Ease easeOut, InterpolationType interpolationType, double t);

        /// <summary>
        /// プロパティの次元を分割します
        /// </summary>
        /// <param name="propertyValue">分割対象のプロパティの値</param>
        /// <returns>分割後のプロパティの値</returns>
        object[] SplitDimension(object propertyValue) => Array.Empty<object>();

        /// <summary>
        /// 次元の分割後の各プロパティの編集用コントロールを生成します
        /// </summary>
        /// <returns>各次元に対応したPropertyControl</returns>
        PropertyControlBase[] CreateSplitedDimensionControl() => Array.Empty<PropertyControlBase>();

        /// <summary>
        /// 他のプロパティの値から現在のプロパティの値に変更します。値が対応していない型の場合、InvalidOperationExceptionをthrowします
        /// </summary>
        /// <param name="otherValue">変換元のプロパティの値</param>
        /// <returns>返還後のプロパティの値</returns>
        /// <exception cref="InvalidOperationException">値の変換に対応していません</exception>
        object ConvertFrom(object otherValue);
    }
}
