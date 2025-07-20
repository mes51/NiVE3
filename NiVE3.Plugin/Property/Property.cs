using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Interaction;
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
        public string DisplayName => DisplayNameKey?.GetText() ?? RawDisplayName ?? "";

        string? RawDisplayName { get; }

        LanguageResourceKey? DisplayNameKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">プロパティの名前</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        /// <param name="isSupportKeyFrame">キーフレームをサポートするかどうか</param>
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
        /// <param name="isSupportKeyFrame">キーフレームをサポートするかどうか</param>
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
        /// <returns>プロパティを操作するコントロール</returns>
        public abstract PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel);

        /// <summary>
        /// 値をこのプロパティの範囲、型に変更します
        /// </summary>
        /// <param name="value">変更対象の値</param>
        /// <returns>変更後の値</returns>
        public abstract object? CoerceValue(object? value);

        /// <summary>
        /// プロパティの表示管理用のステートを生成します
        /// </summary>
        /// <param name="composition">このプロパティが含まれるコンポジション</param>
        /// <param name="layer">このプロパティが含まれるレイヤー。コンポジションのプロパティの場合はnull</param>
        /// <param name="effect">このプロパティが含まれるエフェクト。レイヤーのプロパティやコンポジションのプロパティの場合はnull</param>
        /// <param name="viewModel">このプロパティのViewModel</param>
        /// <returns>ステートを管理するPropertyViewState</returns>
        public virtual PropertyViewState CreateState(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            return new PropertyViewState(DisplayName);
        }

        /// <summary>
        /// プレビューパネルからプロパティを操作するためのPropertyInteractionを生成します
        /// </summary>
        /// <param name="viewModel">このプロパティのPropertyInteraction用のViewModel</param>
        /// <returns>プロパティを操作するためのPropertyInteraction。プレビューパネルからの操作に対応しない場合はnull</returns>
        public virtual PropertyInteractionBase? CreatePropertyInteraction(IPropertyInteractionViewModel viewModel)
        {
            return null;
        }
    }

    public abstract class LayerDependPropertyBase : PropertyBase
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">プロパティの名前</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        protected LayerDependPropertyBase(string id, string displayName, IPropertyType propertyType, object? defaultValue) : base(id, displayName, propertyType, defaultValue, false) { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayNameKey">プロパティの名前のLanguageResourceKey</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        protected LayerDependPropertyBase(string id, LanguageResourceKey displayNameKey, IPropertyType propertyType, object? defaultValue) : base(id, displayNameKey, propertyType, defaultValue, false) { }

        /// <summary>
        /// 値がこのプロパティで使用できるか検証します
        /// </summary>
        /// <param name="value">検証対象の値</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>使用できる場合はtrue、出来ない場合はfalse</returns>
        public abstract bool ValidateValue(object? value, ILayerObject layer);

        /// <summary>
        /// 値をこのプロパティの範囲、型に変更します
        /// </summary>
        /// <param name="value">変更対象の値</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>変更後の値</returns>
        public abstract object? CoerceValue(object? value, ILayerObject layer);

        /// <summary>
        /// レイヤーに変更があった事による値の更新を行います
        /// </summary>
        /// <param name="value">更新前の値</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>変更後の値</returns>
        public abstract object? ChangeValueByLayerStateChanged(object? value, ILayerObject layer);

        /// <summary>
        /// マスクのペーストなどでMaskIdが変更された際に値を更新行います
        /// </summary>
        /// <param name="value">更新前の値</param>
        /// <param name="maskIdMap">MaskIdと新しいMaskIdのマップ。変更がなかった場合はマップに含まれません</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>変更後の値</returns>
        public abstract object? ChangeValueByReplaceMaskId(object? value, Dictionary<Guid, Guid> maskIdMap, ILayerObject layer);

        /// <summary>
        /// エフェクトのペーストなどでEffectIdが変更された際に値を更新行います
        /// </summary>
        /// <param name="value">更新前の値</param>
        /// <param name="effectIdMap">EffectIdと新しいEffectIdのマップ。変更がなかった場合はマップに含まれません</param>
        /// <param name="layer">レイヤー</param>
        /// <returns>変更後の値</returns>
        public abstract object? ChangeValueByReplaceEffectId(object? value, Dictionary<Guid, Guid> effectIdMap, ILayerObject layer);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override object? CoerceValue(object? value)
        {
            return value;
        }
    }

    /// <summary>
    /// コンポジションの状態に依存するプロパティを表します
    /// </summary>
    public abstract class CompositionDependPropertyBase : PropertyBase
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">プロパティの名前</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        protected CompositionDependPropertyBase(string id, string displayName, IPropertyType propertyType, object? defaultValue) : base(id, displayName, propertyType, defaultValue, false) { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayNameKey">プロパティの名前のLanguageResourceKey</param>
        /// <param name="propertyType">使用するPropertyType</param>
        /// <param name="defaultValue">デフォルトの値</param>
        protected CompositionDependPropertyBase(string id, LanguageResourceKey displayNameKey, IPropertyType propertyType, object? defaultValue) : base(id, displayNameKey, propertyType, defaultValue, false) { }

        /// <summary>
        /// 値がこのプロパティで使用できるか検証します
        /// </summary>
        /// <param name="value">検証対象の値</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>使用できる場合はtrue、出来ない場合はfalse</returns>
        public abstract bool ValidateValue(object? value, ICompositionObject composition);

        /// <summary>
        /// 値をこのプロパティの範囲、型に変更します
        /// </summary>
        /// <param name="value">変更対象の値</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>変更後の値</returns>
        public abstract object? CoerceValue(object? value, ICompositionObject composition);

        /// <summary>
        /// コンポジションに変更があった事による値の更新を行います
        /// </summary>
        /// <param name="value">更新前の値</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>変更後の値</returns>
        public abstract object? ChangeValueByCompositionStateChanged(object? value, ICompositionObject composition);

        /// <summary>
        /// レイヤーのペーストなどでLayerIdが変更された際に値を更新行います
        /// </summary>
        /// <param name="value">更新前の値</param>
        /// <param name="layerIdMap">LayerIdと新しいLayerIdのマップ。変更がなかった場合はマップに含まれません</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>変更後の値</returns>
        public abstract object? ChangeValueByReplaceLayerId(object? value, Dictionary<Guid, Guid> layerIdMap, ICompositionObject composition);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override object? CoerceValue(object? value)
        {
            return value;
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
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">グループの名前</param>
        /// <param name="children">グループに含まれるプロパティ</param>
        public PropertyGroup(string id, string displayName, PropertyBase[] children) : base(id, displayName, PropertyGroupType.Instance, null, false)
        {
            Children = children;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayNameKey">グループの名前のLanguageResourceKey</param>
        /// <param name="children">グループに含まれるプロパティ</param>
        public PropertyGroup(string id, LanguageResourceKey displayNameKey, PropertyBase[] children) : base(id, displayNameKey, PropertyGroupType.Instance, null, false)
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
        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override object? CoerceValue(object? value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ユーザーが指定した子プロパティを追加できるプロパティを表します
    /// </summary>
    public class AppendableProperty : PropertyBase
    {
        public AppendablePropertyItem[] Items { get; }

        public AppendablePropertyItem? DefaultAppendedItem { get; }

        public bool UseEnableSwitch { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">親プロパティの名前</param>
        /// <param name="items">このプロパティに追加可能な子プロパティの生成メソッド</param>
        /// <param name="defaultAppendedItemIndex">初期状態で追加されている子プロパティのインデックス</param>
        /// <param name="useEnableSwitch">子プロパティに有効/無効スイッチを表示するかどうか</param>
        public AppendableProperty(string id, string displayName, AppendablePropertyItem[] items, int? defaultAppendedItemIndex = null, bool useEnableSwitch = false) : base(id, displayName, AppendablePropertyType.Instance, null, false)
        {
            Items = items;
            if (defaultAppendedItemIndex != null)
            {
                DefaultAppendedItem = items[defaultAppendedItemIndex.Value];
            }
            UseEnableSwitch = useEnableSwitch;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayNameKey">親プロパティの名前のLanguageResourceKey</param>
        /// <param name="items">このプロパティに追加可能な子プロパティの生成メソッド</param>
        /// <param name="defaultAppendedItemIndex">初期状態で追加されている子プロパティのインデックス</param>
        /// <param name="useEnableSwitch">子プロパティに有効/無効スイッチを表示するかどうか</param>
        public AppendableProperty(string id, LanguageResourceKey displayNameKey, AppendablePropertyItem[] items, int? defaultAppendedItemIndex = null, bool useEnableSwitch = false) : base(id, displayNameKey, AppendablePropertyType.Instance, null, false)
        {
            Items = items;
            if (defaultAppendedItemIndex != null)
            {
                DefaultAppendedItem = items[defaultAppendedItemIndex.Value];
            }
            UseEnableSwitch = useEnableSwitch;
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="composition"></param>
        /// <param name="layer"></param>
        /// <param name="effect"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用しません
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override object? CoerceValue(object? value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 追加可能なプロパティの名前と生成処理を表します
    /// </summary>
    public class AppendablePropertyItem
    {
        public string Id { get; }

        public Func<PropertyGroup> CreateFunc { get; }

        public string DisplayName => DisplayNameKey?.GetText() ?? RawDisplayName ?? "";

        public bool IsSeparator { get; protected init; }

        string? RawDisplayName { get; }

        LanguageResourceKey? DisplayNameKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">子プロパティのルートグループのID。他AppendablePropertyItemと被らないようユニークである必要があります</param>
        /// <param name="name">プロパティの名前</param>
        /// <param name="createFunc">プロパティを生成するメソッド</param>
        public AppendablePropertyItem(string id, string name, Func<PropertyGroup> createFunc)
        {
            Id = id;
            RawDisplayName = name;
            CreateFunc = createFunc;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">子プロパティのルートグループのID。他AppendablePropertyItemと被らないようユニークである必要があります</param>
        /// <param name="nameKey">プロパティの名前のLanguageResourceKey</param>
        /// <param name="createFunc">プロパティを生成するメソッド</param>
        public AppendablePropertyItem(string id, LanguageResourceKey nameKey, Func<PropertyGroup> createFunc)
        {
            Id = id;
            DisplayNameKey = nameKey;
            CreateFunc = createFunc;
        }
    }

    /// <summary>
    /// 追加可能なプロパティの一覧に表示するセパレーターを表します
    /// </summary>
    public class AppendablePropertyItemSeparator : AppendablePropertyItem
    {
        public static readonly AppendablePropertyItemSeparator Instance = new AppendablePropertyItemSeparator();

        private AppendablePropertyItemSeparator() : base("", "", () => new PropertyGroup("", "", []))
        {
            IsSeparator = true;
        }
    }
}
