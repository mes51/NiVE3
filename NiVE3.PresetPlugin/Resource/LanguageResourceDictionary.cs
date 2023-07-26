using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.PresetPlugin.Resource
{
    class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        [DefaultValue("ポイント制御")]
        public const string ExpressionControl_PointControl_Name = nameof(ExpressionControl_PointControl_Name);

        [DefaultValue("エクスプレッションで使用するポイント制御")]
        public const string ExpressionControl_PointControl_Description = nameof(ExpressionControl_PointControl_Description);

        [DefaultValue("スライダー制御")]
        public const string ExpressionControl_SliderControl_Name = nameof(ExpressionControl_SliderControl_Name);

        [DefaultValue("エクスプレッションで使用するスライダー制御")]
        public const string ExpressionControl_SliderControl_Description = nameof(ExpressionControl_SliderControl_Description);

        [DefaultValue("デフォルトレンダラ")]
        public const string Renderer_DefaultRenderer_Name = nameof(Renderer_DefaultRenderer_Name);

        [DefaultValue("NiVE標準のレンダラ")]
        public const string Renderer_DefaultRenderer_Description = nameof(Renderer_DefaultRenderer_Description);

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                .Select(f => (f.Name, f.GetCustomAttribute<DefaultValueAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Name, t => Tuple.Create(t.Item2!.DefaultValue, Version.Parse(t.Item2!.FromVersion)));
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
