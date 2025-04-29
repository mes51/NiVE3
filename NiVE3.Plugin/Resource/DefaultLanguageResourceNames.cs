using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Resource
{
    public static class DefaultLanguageResourceNames
    {
        public static ImmutableArray<string> EffectCategories { get; }

        /// <summary>
        /// エフェクトのオーディオカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Audio = nameof(EffectCategory_Audio);

        /// <summary>
        /// エフェクトのブラーカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Blur = nameof(EffectCategory_Blur);

        /// <summary>
        /// エフェクトのチャンネルカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Channel = nameof(EffectCategory_Channel);

        /// <summary>
        /// エフェクトのカラー補正カテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_ColorCollection = nameof(EffectCategory_ColorCollection);

        /// <summary>
        /// エフェクトのディストーションカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Distortion = nameof(EffectCategory_Distortion);

        /// <summary>
        /// エフェクトのエクスプレッション制御カテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_ExpressionControl = nameof(EffectCategory_ExpressionControl);

        /// <summary>
        /// エフェクトの描画カテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Generate = nameof(EffectCategory_Generate);

        /// <summary>
        /// エフェクトのキーイングカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Keying = nameof(EffectCategory_Keying);

        /// <summary>
        /// エフェクトのノイズカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Noise = nameof(EffectCategory_Noise);

        /// <summary>
        /// エフェクトのシミュレーションカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Simulation = nameof(EffectCategory_Simulation);

        /// <summary>
        /// エフェクトのスタイライズカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Stylize = nameof(EffectCategory_Stylize);

        /// <summary>
        /// エフェクトのユーティリティカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Utility = nameof(EffectCategory_Utility);

        static DefaultLanguageResourceNames()
        {
            EffectCategories = [..typeof(DefaultLanguageResourceNames).GetFields().Where(f => f.IsDefined(typeof(EffectCategoryAttribute))).Select(f => f.Name)];
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    file class EffectCategoryAttribute : Attribute
    {
        public EffectCategoryAttribute() { }
    }
}
