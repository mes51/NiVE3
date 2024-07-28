using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Control.Resources
{
    class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        public static LanguageResourceDictionary Dictionary { get; }

        [DefaultValue("なし")]
        public const string PropertyControl_UseLayerImagePropertyControl_None = nameof(PropertyControl_UseLayerImagePropertyControl_None);

        // Enum

        [DefaultValue("ソース")]
        public const string LayerImageProcessType_Raw = nameof(LayerImageProcessType_Raw);

        [DefaultValue("マスク")]
        public const string LayerImageProcessType_Masked = nameof(LayerImageProcessType_Masked);

        [DefaultValue("エフェクト+マスク")]
        public const string LayerImageProcessType_Effected = nameof(LayerImageProcessType_Effected);

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

        protected override void Reload()
        {
            // TODO: 言語情報読み込み&バージョン比較後適用
            foreach (var (key, (defaultValue, version)) in LanguageKeys)
            {
                this[key] = defaultValue;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    file sealed class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(string defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public string DefaultValue { get; }

        public string FromVersion { get; set; } = "0.0.0.0";
    }
}
