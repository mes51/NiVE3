using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Internal.View.Resources
{
    // NOTE: MarkupableResourceDictionaryはNiVE3.SourceGenerator.ResourceMarkupGeneratorを参照すると
    //       InternalsVisibleToを使用している関係か競合するので使用しない
    class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        [DefaultValue("位置:")]
        public static readonly string ColorGradientDialog_PointPosition = nameof(ColorGradientDialog_PointPosition);

        [DefaultValue("不透明度の分岐点")]
        public static readonly string ColorGradientDialog_OpacityGroup_Name = nameof(ColorGradientDialog_OpacityGroup_Name);

        [DefaultValue("不透明度:")]
        public static readonly string ColorGradientDialog_ColorGradientDialog_OpacityGroup_Opacity = nameof(ColorGradientDialog_ColorGradientDialog_OpacityGroup_Opacity);

        [DefaultValue("色の分岐点")]
        public static readonly string ColorGradientDialog_ColorGroup_Name = nameof(ColorGradientDialog_ColorGroup_Name);

        [DefaultValue("OKLab色空間での補間をプレビューする")]
        public static readonly string ColorGradientDialog_UseOkLabInterpolation = nameof(ColorGradientDialog_UseOkLabInterpolation);

        [DefaultValue("なし")]
        public const string PropertyControl_UseLayerImagePropertyControl_None = nameof(PropertyControl_UseLayerImagePropertyControl_None);

        [DefaultValue("OK")]
        public static readonly string Dialog_OK = nameof(Dialog_OK);

        [DefaultValue("キャンセル")]
        public static readonly string Dialog_Cancel = nameof(Dialog_Cancel);

        [DefaultValue("%")]
        public static readonly string Unit_Percent = nameof(Unit_Percent);

        // Enum

        [DefaultValue("ソース")]
        public const string LayerImageProcessType_Raw = nameof(LayerImageProcessType_Raw);

        [DefaultValue("マスク")]
        public const string LayerImageProcessType_Masked = nameof(LayerImageProcessType_Masked);

        [DefaultValue("エフェクト")]
        public const string LayerImageProcessType_Effected = nameof(LayerImageProcessType_Effected);

        public static LanguageResourceDictionary Dictionary { get; }

        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => (f.Name, f.GetCustomAttribute<DefaultValueAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Name, t => Tuple.Create(t.Item2!.DefaultValue, Version.Parse(t.Item2!.FromVersion)));

            Dictionary = [];
        }

        public LanguageResourceDictionary()
        {
            Reload();
        }

        protected override void Reload(string? forceLangCode = null)
        {
            // TODO: 言語情報読み込み&バージョン比較後適用
            foreach (var (key, (defaultValue, version)) in LanguageKeys)
            {
                this[key] = defaultValue;
            }
        }

        public static LanguageResourceKey CreateLanguageResourceKey(string key)
        {
            return new LanguageResourceKey(typeof(LanguageResourceDictionary), key);
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class DefaultValueAttribute : Attribute
        {
            public DefaultValueAttribute(string defaultValue)
            {
                DefaultValue = defaultValue;
            }

            public string DefaultValue { get; }

            public string FromVersion { get; set; } = "0.0.0.0";
        }
    }
}
