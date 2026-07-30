using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.OpenFX.Host;
using NiVE3.OpenFX.Interop;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.OpenFX.Bridge
{
    /// <summary>
    /// OFX のパラメータと NiVE3 のプロパティの相互変換
    /// </summary>
    public static class OfxParamBridge
    {
        // DoubleProperty が有限の範囲を要求するため、OFX の実質無限の範囲はこの値へ丸める
        const double FallbackRange = 1.0e9;

        /// <summary>
        /// エフェクトインスタンスのパラメータから NiVE3 のプロパティ一覧を生成します
        /// </summary>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="buttonClicked">PushButton がクリックされた際に呼ばれるコールバック (引数はパラメータ名)</param>
        /// <returns>生成されたプロパティの一覧 (Group/Page は PropertyGroup として階層化)</returns>
        public static PropertyBase[] BuildProperties(EffectInstance instance, Action<string>? buttonClicked = null)
        {
            // Page の子は Parent ではなく、Page 自身の OfxParamPropPageChild で列挙される
            var pages = instance.Params.Params.Where(p => p.ParamType == OfxNames.ParamTypePage).ToArray();
            var pageAssignments = new Dictionary<string, string>();
            // Page が 1 つだけの場合は既定のページとみなし、階層化せずルートへ展開する
            if (pages.Length > 1)
            {
                foreach (var page in pages)
                {
                    var childCount = page.Properties.GetDimension(OfxNames.ParamPropPageChild);
                    for (var i = 0; i < childCount; i++)
                    {
                        if (page.Properties.GetOrDefault(OfxNames.ParamPropPageChild, i) is string child &&
                            child.Length > 0 &&
                            !child.StartsWith("OfxParamPageSkip", StringComparison.Ordinal))
                        {
                            pageAssignments.TryAdd(child, page.Name);
                        }
                    }
                }
            }

            // Parent プロパティ (無ければ Page への所属) による階層を構築する
            var childrenByParent = new Dictionary<string, List<ParamInstance>>();
            foreach (var param in instance.Params.Params)
            {
                if (pages.Length == 1 && param.ParamType == OfxNames.ParamTypePage)
                {
                    continue;
                }

                var parent = GetString(param, OfxNames.ParamPropParent, "");
                if (parent.Length < 1 && pageAssignments.TryGetValue(param.Name, out var pageName))
                {
                    parent = pageName;
                }
                if (!childrenByParent.TryGetValue(parent, out var list))
                {
                    list = new List<ParamInstance>();
                    childrenByParent[parent] = list;
                }
                list.Add(param);
            }

            return BuildChildren("", childrenByParent, instance, buttonClicked, new OfxParamViewStateBinder());
        }

        static PropertyBase[] BuildChildren(string parentName, Dictionary<string, List<ParamInstance>> childrenByParent, EffectInstance instance, Action<string>? buttonClicked, OfxParamViewStateBinder binder)
        {
            if (!childrenByParent.TryGetValue(parentName, out var children))
            {
                return [];
            }

            var result = new List<PropertyBase>();
            foreach (var param in children)
            {
                // Secret のパラメータも生成し、PropertyViewState (IsVisible) で非表示にする
                // (プラグインが実行時に Secret を切り替えた際に表示できるようにするため)
                var property = CreateProperty(param, childrenByParent, instance, buttonClicked, binder);
                if (property != null)
                {
                    result.Add(property);
                }
            }
            return result.ToArray();
        }

        static PropertyBase? CreateProperty(ParamInstance param, Dictionary<string, List<ParamInstance>> childrenByParent, EffectInstance instance, Action<string>? buttonClicked, OfxParamViewStateBinder binder)
        {
            var label = GetString(param, OfxNames.PropLabel, param.Name);
            var animates = GetInt(param, OfxNames.ParamPropAnimates, 0) != 0;
            var isPersistent = GetInt(param, OfxNames.ParamPropPersistant, 1) != 0;
            Func<PropertyViewState> viewStateFactory = () => binder.CreateState(param, label);

            switch (param.ParamType)
            {
                case OfxNames.ParamTypeGroup:
                case OfxNames.ParamTypePage:
                {
                    // 中身が空になったグループ/ページは表示しない
                    var children = BuildChildren(param.Name, childrenByParent, instance, buttonClicked, binder);
                    return children.Length > 0
                        ? new PropertyGroup(param.Name, label, children) { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent }
                        : null;
                }

                case OfxNames.ParamTypeDouble:
                {
                    var (min, max) = GetRange(param, isInteger: false);
                    return new DoubleProperty(param.Name, label, GetDouble(param, OfxNames.ParamPropDefault, 0, 0.0), min, max, animates,
                        slideChangeValue: GetDouble(param, OfxNames.ParamPropIncrement, 0, 1.0),
                        digit: Math.Max(GetInt(param, OfxNames.ParamPropDigits, 2), 0))
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypeInteger:
                {
                    var (min, max) = GetRange(param, isInteger: true);
                    return new DoubleProperty(param.Name, label, GetInt(param, OfxNames.ParamPropDefault, 0), min, max, animates, slideChangeValue: 1.0, digit: 0)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypeDouble2D:
                case OfxNames.ParamTypeDouble3D:
                case OfxNames.ParamTypeInteger2D:
                case OfxNames.ParamTypeInteger3D:
                {
                    var isInteger = param.ParamType is OfxNames.ParamTypeInteger2D or OfxNames.ParamTypeInteger3D;
                    var is3D = param.ParamType is OfxNames.ParamTypeDouble3D or OfxNames.ParamTypeInteger3D;
                    var defaultValue = new Vector3d(
                        GetDouble(param, OfxNames.ParamPropDefault, 0, 0.0),
                        GetDouble(param, OfxNames.ParamPropDefault, 1, 0.0),
                        is3D ? GetDouble(param, OfxNames.ParamPropDefault, 2, 0.0) : 0.0);
                    var (min, max) = GetRange(param, isInteger);
                    return new Vector3dProperty(param.Name, label, defaultValue, new Vector3d(min), new Vector3d(max), animates,
                        digit: isInteger ? 0 : Math.Max(GetInt(param, OfxNames.ParamPropDigits, 2), 0), is3D: is3D)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypeRGB:
                case OfxNames.ParamTypeRGBA:
                {
                    // NiVE3 の色は Vector4 (B, G, R, A)
                    var defaultValue = new Vector4(
                        (float)GetDouble(param, OfxNames.ParamPropDefault, 2, 0.0),
                        (float)GetDouble(param, OfxNames.ParamPropDefault, 1, 0.0),
                        (float)GetDouble(param, OfxNames.ParamPropDefault, 0, 0.0),
                        param.ParamType == OfxNames.ParamTypeRGBA ? (float)GetDouble(param, OfxNames.ParamPropDefault, 3, 1.0) : 1.0F);
                    return new ColorProperty(param.Name, label, label, "OK", "Cancel", defaultValue, animates)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypeBoolean:
                    return new CheckBoxProperty(param.Name, label, GetInt(param, OfxNames.ParamPropDefault, 0) != 0, animates)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };

                case OfxNames.ParamTypeChoice:
                {
                    var optionCount = param.Properties.GetDimension(OfxNames.ParamPropChoiceOption);
                    if (optionCount < 1)
                    {
                        return null;
                    }
                    var options = Enumerable.Range(0, optionCount)
                        .Select(i => param.Properties.TryGet(OfxNames.ParamPropChoiceOption, i, out var v) ? v as string ?? $"({i})" : $"({i})")
                        .ToArray();
                    return new SelectBoxProperty(param.Name, label, options, GetInt(param, OfxNames.ParamPropDefault, 0), animates)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypeString:
                {
                    var isMultiLine = param.Properties.GetOrDefault(OfxNames.ParamPropStringMode, 0) as string == OfxNames.ParamStringIsMultiLine;
                    return new StringProperty(param.Name, label, param.Values.ElementAtOrDefault(0) as string ?? "",
                        isReadOnly: GetInt(param, OfxNames.ParamPropEnabled, 1) == 0,
                        textBoxWidth: isMultiLine ? 300.0 : 200.0,
                        isMultiLine: isMultiLine)
                    { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                }

                case OfxNames.ParamTypePushButton:
                {
                    var button = new ButtonProperty(param.Name, label) { ViewStateFactory = viewStateFactory, IsPersistent = isPersistent };
                    if (buttonClicked != null)
                    {
                        var paramName = param.Name;
                        button.Clicked += (_, _) => buttonClicked(paramName);
                    }
                    return button;
                }

                default:
                    OfxLog.Warn($"未対応のパラメータ型のためスキップします: {param.Name} ({param.ParamType})");
                    return null;
            }
        }

        /// <summary>
        /// NiVE3 のプロパティの値をエフェクトインスタンスのパラメータへ反映します
        /// レンダリングや InstanceChanged の前に呼び出します
        /// </summary>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="properties">エフェクトのプロパティ一覧 (グループ階層可)</param>
        /// <param name="layerTime">値を取得するレイヤー時間</param>
        public static void ApplyValues(EffectInstance instance, IReadOnlyCollection<IPropertyObject> properties, Time layerTime)
        {
            var flattened = new Dictionary<string, IPropertyObject>();
            Flatten(properties, flattened);

            foreach (var param in instance.Params.Params)
            {
                if (param.ValueKind == OfxParamValueKind.None || !flattened.TryGetValue(param.Name, out var property))
                {
                    continue;
                }

                var value = property.GetValue(layerTime);
                switch (param.ParamType)
                {
                    case OfxNames.ParamTypeDouble:
                        param.SetValue(0, OfxParamTypes.ToDouble(value ?? 0.0));
                        break;
                    case OfxNames.ParamTypeInteger:
                        param.SetValue(0, (int)Math.Round(OfxParamTypes.ToDouble(value ?? 0.0)));
                        break;
                    case OfxNames.ParamTypeDouble2D:
                    case OfxNames.ParamTypeDouble3D:
                    case OfxNames.ParamTypeInteger2D:
                    case OfxNames.ParamTypeInteger3D:
                        if (value is Vector3d vector)
                        {
                            var isInteger = param.ParamType is OfxNames.ParamTypeInteger2D or OfxNames.ParamTypeInteger3D;
                            for (var i = 0; i < param.Dimension; i++)
                            {
                                var component = i switch { 0 => vector.X, 1 => vector.Y, _ => vector.Z };
                                if (isInteger)
                                {
                                    param.SetValue(i, (int)Math.Round(component));
                                }
                                else
                                {
                                    param.SetValue(i, component);
                                }
                            }
                        }
                        break;
                    case OfxNames.ParamTypeRGB:
                    case OfxNames.ParamTypeRGBA:
                        if (value is Vector4 color)
                        {
                            // Vector4 (B, G, R, A) → OFX (R, G, B, A)
                            param.SetValue(0, (double)color.Z);
                            param.SetValue(1, (double)color.Y);
                            param.SetValue(2, (double)color.X);
                            if (param.Dimension > 3)
                            {
                                param.SetValue(3, (double)color.W);
                            }
                        }
                        break;
                    case OfxNames.ParamTypeBoolean:
                        param.SetValue(0, value is bool b && b ? 1 : 0);
                        break;
                    case OfxNames.ParamTypeChoice:
                        param.SetValue(0, OfxParamTypes.ToInt(value ?? 0));
                        break;
                    case OfxNames.ParamTypeString:
                        param.SetValue(0, value as string ?? "");
                        break;
                }
            }
        }

        /// <summary>
        /// パラメータの現在値を NiVE3 のプロパティ値へ変換します (ApplyValues の逆変換)
        /// </summary>
        /// <param name="param">対象のパラメータ</param>
        /// <returns>NiVE3 のプロパティ値。値を持たないパラメータの場合は null</returns>
        public static object? ConvertToPropertyValue(ParamInstance param)
        {
            switch (param.ParamType)
            {
                case OfxNames.ParamTypeDouble:
                case OfxNames.ParamTypeInteger:
                    return OfxParamTypes.ToDouble(param.Values[0] ?? 0.0);
                case OfxNames.ParamTypeDouble2D:
                case OfxNames.ParamTypeInteger2D:
                    return new Vector3d(OfxParamTypes.ToDouble(param.Values[0] ?? 0.0), OfxParamTypes.ToDouble(param.Values[1] ?? 0.0), 0.0);
                case OfxNames.ParamTypeDouble3D:
                case OfxNames.ParamTypeInteger3D:
                    return new Vector3d(OfxParamTypes.ToDouble(param.Values[0] ?? 0.0), OfxParamTypes.ToDouble(param.Values[1] ?? 0.0), OfxParamTypes.ToDouble(param.Values[2] ?? 0.0));
                case OfxNames.ParamTypeRGB:
                case OfxNames.ParamTypeRGBA:
                    // OFX (R, G, B, A) → NiVE3 の Vector4 (B, G, R, A)
                    return new Vector4(
                        (float)OfxParamTypes.ToDouble(param.Values.ElementAtOrDefault(2) ?? 0.0),
                        (float)OfxParamTypes.ToDouble(param.Values.ElementAtOrDefault(1) ?? 0.0),
                        (float)OfxParamTypes.ToDouble(param.Values.ElementAtOrDefault(0) ?? 0.0),
                        param.Dimension > 3 ? (float)OfxParamTypes.ToDouble(param.Values.ElementAtOrDefault(3) ?? 1.0) : 1.0F);
                case OfxNames.ParamTypeBoolean:
                    return OfxParamTypes.ToInt(param.Values[0] ?? 0) != 0;
                case OfxNames.ParamTypeChoice:
                    return OfxParamTypes.ToInt(param.Values[0] ?? 0);
                case OfxNames.ParamTypeString:
                case OfxNames.ParamTypeCustom:
                    return param.Values[0] as string ?? "";
                default:
                    return null;
            }
        }

        static void Flatten(IReadOnlyCollection<IPropertyObject> properties, Dictionary<string, IPropertyObject> result)
        {
            foreach (var property in properties)
            {
                result[property.Id] = property;
                var children = property.GetChildren();
                if (children != null)
                {
                    Flatten(children, result);
                }
            }
        }

        static (double Min, double Max) GetRange(ParamInstance param, bool isInteger)
        {
            var min = GetFiniteRangeValue(param, 0, -FallbackRange, isInteger);
            var max = GetFiniteRangeValue(param, 1, FallbackRange, isInteger);
            if (min >= max)
            {
                return (-FallbackRange, FallbackRange);
            }
            return (min, max);
        }

        static double GetFiniteRangeValue(ParamInstance param, int side, double fallback, bool isInteger)
        {
            // DisplayMin/DisplayMax を優先し、無限相当の値は fallback に丸める
            var displayKey = side == 0 ? OfxNames.ParamPropDisplayMin : OfxNames.ParamPropDisplayMax;
            var hardKey = side == 0 ? OfxNames.ParamPropMin : OfxNames.ParamPropMax;

            foreach (var key in (ReadOnlySpan<string>)[displayKey, hardKey])
            {
                if (param.Properties.GetOrDefault(key, 0) is object value)
                {
                    var v = OfxParamTypes.ToDouble(value);
                    var nearInfinite = isInteger ? Math.Abs(v) >= int.MaxValue : Math.Abs(v) >= FallbackRange;
                    if (double.IsFinite(v) && !nearInfinite)
                    {
                        return v;
                    }
                }
            }
            return fallback;
        }

        static string GetString(ParamInstance param, string key, string defaultValue)
        {
            return param.Properties.GetOrDefault(key, 0) as string ?? defaultValue;
        }

        static int GetInt(ParamInstance param, string key, int defaultValue)
        {
            return param.Properties.GetOrDefault(key, 0) is object value ? OfxParamTypes.ToInt(value) : defaultValue;
        }

        static double GetDouble(ParamInstance param, string key, int index, double defaultValue)
        {
            return param.Properties.GetOrDefault(key, index) is object value ? OfxParamTypes.ToDouble(value) : defaultValue;
        }
    }
}
