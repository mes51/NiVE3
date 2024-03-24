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

        [DefaultValue("ポイント")]
        public const string ExpressionControl_PointControl_PropertyName = nameof(ExpressionControl_PointControl_PropertyName);

        [DefaultValue("エクスプレッションで使用するポイント制御")]
        public const string ExpressionControl_PointControl_Description = nameof(ExpressionControl_PointControl_Description);

        [DefaultValue("スライダー制御")]
        public const string ExpressionControl_SliderControl_Name = nameof(ExpressionControl_SliderControl_Name);

        [DefaultValue("スライダー")]
        public const string ExpressionControl_SliderControl_PropertyName = nameof(ExpressionControl_SliderControl_PropertyName);

        [DefaultValue("エクスプレッションで使用するスライダー制御")]
        public const string ExpressionControl_SliderControl_Description = nameof(ExpressionControl_SliderControl_Description);

        [DefaultValue("ボックスブラー")]
        public const string Blur_BoxBlur_Name = nameof(Blur_BoxBlur_Name);

        [DefaultValue("ボックスブラーを適用します")]
        public const string Blur_BoxBlur_Description = nameof(Blur_BoxBlur_Description);

        [DefaultValue("半径")]
        public const string Blur_BoxBlur_Amount = nameof(Blur_BoxBlur_Amount);

        [DefaultValue("繰り返し")]
        public const string Blur_BoxBlur_Repeat = nameof(Blur_BoxBlur_Repeat);

        [DefaultValue("方向")]
        public const string Blur_BoxBlur_Direction = nameof(Blur_BoxBlur_Direction);

        [DefaultValue("ダイナミクス")]
        public const string Audio_Dynamics_Name = nameof(Audio_Dynamics_Name);

        [DefaultValue("音声のダイナミクスを操作します")]
        public const string Audio_Dynamics_Description = nameof(Audio_Dynamics_Description);

        [DefaultValue("タイプ")]
        public const string Audio_Dynamics_DynamicsProcessorType = nameof(Audio_Dynamics_DynamicsProcessorType);

        [DefaultValue("スレッシュホールド")]
        public const string Audio_Dynamics_Threshold = nameof(Audio_Dynamics_Threshold);

        [DefaultValue("レシオ")]
        public const string Audio_Dynamics_Ratio = nameof(Audio_Dynamics_Ratio);

        [DefaultValue("ゲイン")]
        public const string Audio_Dynamics_Gain = nameof(Audio_Dynamics_Gain);

        [DefaultValue("アタック")]
        public const string Audio_Dynamics_Attack = nameof(Audio_Dynamics_Attack);

        [DefaultValue("リリース")]
        public const string Audio_Dynamics_Release = nameof(Audio_Dynamics_Release);

        [DefaultValue("デフォルトレンダラ")]
        public const string Renderer_DefaultRenderer_Name = nameof(Renderer_DefaultRenderer_Name);

        [DefaultValue("NiVE標準のレンダラ")]
        public const string Renderer_DefaultRenderer_Description = nameof(Renderer_DefaultRenderer_Description);

        // enum

        [DefaultValue("水平&垂直")]
        public const string BlurDirection_HorizontalAndVertical = nameof(BlurDirection_HorizontalAndVertical);

        [DefaultValue("水平")]
        public const string BlurDirection_Horizontal = nameof(BlurDirection_Horizontal);

        [DefaultValue("垂直")]
        public const string BlurDirection_Vertical = nameof(BlurDirection_Vertical);

        [DefaultValue("コンプレッサー")]
        public const string DynamicsMode_Compressor = nameof(DynamicsMode_Compressor);

        [DefaultValue("リミッター")]
        public const string DynamicsMode_Limiter = nameof(DynamicsMode_Limiter);

        [DefaultValue("エクスパンダー")]
        public const string DynamicsMode_Expander = nameof(DynamicsMode_Expander);

        [DefaultValue("ノイズゲート")]
        public const string DynamicsMode_NoiseGate = nameof(DynamicsMode_NoiseGate);

        // unit

        [DefaultValue("ms")]
        public const string Unit_MilliSecond = nameof(Unit_MilliSecond);

        [DefaultValue("dB")]
        public const string Unit_Decibel = nameof(Unit_Decibel);

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

        public static LanguageResourceKey CreateResourceKey(string name)
        {
            return new LanguageResourceKey(typeof(LanguageResourceDictionary), name);
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
