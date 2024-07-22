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
        /// エフェクトのカラー補正カテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_ColorCollection = nameof(EffectCategory_ColorCollection);

        /// <summary>
        /// エフェクトのエクスプレッション制御カテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_ExpressionControl = nameof(EffectCategory_ExpressionControl);

        /// <summary>
        /// エフェクトのノイズカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Noise = nameof(EffectCategory_Noise);

        /// <summary>
        /// エフェクトのスタイライズカテゴリを表します
        /// </summary>
        [EffectCategory]
        public const string EffectCategory_Stylize = nameof(EffectCategory_Stylize);

        static DefaultLanguageResourceNames()
        {
            EffectCategories = [..typeof(DefaultLanguageResourceNames).GetFields().Where(f => f.GetCustomAttribute<EffectCategoryAttribute>() != null).Select(f => f.Name)];
        }
    }

    file class EffectCategoryAttribute : Attribute
    {
        public EffectCategoryAttribute() { }
    }
}
