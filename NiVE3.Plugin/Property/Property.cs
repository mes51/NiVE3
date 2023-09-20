using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property
{
    /// <summary>
    /// プロパティを表します
    /// </summary>
    public abstract class PropertyBase
    {
        /// <summary>
        /// エフェクト等内でのプロパティの一意なID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 使用するPropertyType
        /// </summary>
        public IPropertyType PropertyType { get; }

        /// <summary>
        /// キーフレームによる値の補間が使用できるかどうか
        /// </summary>
        public bool IsSupportKeyFrame { get; }

        /// <summary>
        /// プロパティの既定の値
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// プロパティの表示名
        /// </summary>
        protected string DisplayName => DisplayNameKey?.GetText() ?? RawDisplayName ?? "";

        string? RawDisplayName { get; }

        LanguageResourceKey? DisplayNameKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">プロパティの名前</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        public PropertyBase(string id, string displayName, IPropertyType propertyType, object? defaultValue, bool isSupportKeyFrame = true)
        {
            Id = id;
            RawDisplayName = displayName;
            PropertyType = propertyType;
            DefaultValue = defaultValue;
            IsSupportKeyFrame = isSupportKeyFrame;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayNameKey">プロパティの名前のLanguageResourceKey</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        public PropertyBase(string id, LanguageResourceKey displayNameKey, IPropertyType propertyType, object? defaultValue, bool isSupportKeyFrame = true)
        {
            Id = id;
            DisplayNameKey = displayNameKey;
            PropertyType = propertyType;
            DefaultValue = defaultValue;
            IsSupportKeyFrame = isSupportKeyFrame;
        }

        /// <summary>
        /// プロパティを操作するUIを生成します
        /// </summary>
        /// <param name="composition">このプロパティが含まれるコンポジション</param>
        /// <param name="layer">このプロパティが含まれるレイヤー。コンポジションのプロパティの場合はnull</param>
        /// <param name="effect">このプロパティが含まれるエフェクト。レイヤーのプロパティやコンポジションのプロパティの場合はnull</param>
        /// <param name="viewModel">このプロパティのViewModel</param>
        /// <returns></returns>
        public abstract PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel);

        /// <summary>
        /// 値がこのプロパティで使用できるか検証します
        /// </summary>
        /// <param name="value">検証対象の値</param>
        /// <returns>使用できる場合はtrue、出来ない場合はfalse</returns>
        public abstract bool ValidateValue(object value);

        /// <summary>
        /// 値をこのプロパティの範囲、型に変更します
        /// </summary>
        /// <param name="value">変更対象の値</param>
        /// <returns>変更後の値</returns>
        public abstract object CoerceValue(object value);

        /// <summary>
        /// プロパティの表示管理用のステートを生成します
        /// </summary>
        /// <param name="composition">このプロパティが含まれるコンポジション</param>
        /// <param name="layer">このプロパティが含まれるレイヤー。コンポジションのプロパティの場合はnull</param>
        /// <param name="effect">このプロパティが含まれるエフェクト。レイヤーのプロパティやコンポジションのプロパティの場合はnull</param>
        /// <param name="viewModel">このプロパティのViewModel</param>
        /// <returns>ステートを管理するPropertyViewState</returns>
        public virtual PropertyViewState CreateState(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            return new PropertyViewState(DisplayName);
        }
    }

    /// <summary>
    /// プロパティのまとまりを表します
    /// </summary>
    public class PropertyGroup : PropertyBase
    {
        public PropertyBase[] Children { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="displayName">グループの名前</param>
        /// <param name="children">グループに含まれるプロパティ</param>
        public PropertyGroup(string id, string displayName, PropertyBase[] children) : base(id, displayName, PropertyGroupType.Instance, null, false)
        {
            Children = children;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="displayName">グループの名前</param>
        /// <param name="children">グループに含まれるプロパティ</param>
        public PropertyGroup(string id, LanguageResourceKey displayName, PropertyBase[] children) : base(id, displayName, PropertyGroupType.Instance, null, false)
        {
            Children = children;
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="composition"></param>
        /// <param name="layer"></param>
        /// <param name="effect"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool ValidateValue(object value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override object CoerceValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
