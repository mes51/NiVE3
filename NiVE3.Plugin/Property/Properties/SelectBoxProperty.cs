using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    /// <summary>
    /// 実行時に決まる選択肢一覧から選択するプロパティ。値は選択肢のインデックス (int)
    /// </summary>
    public class SelectBoxProperty : PropertyBase
    {
        /// <summary>
        /// 選択肢の表示名一覧。LanguageResourceKey で構築された場合は取得時に現在の言語で解決されます
        /// </summary>
        public IReadOnlyList<string> Options => OptionKeys?.Select(k => k.GetText() ?? "").ToArray() ?? RawOptions!;

        public double SelectBoxWidth { get; }

        IReadOnlyList<string>? RawOptions { get; }

        IReadOnlyList<LanguageResourceKey>? OptionKeys { get; }

        public SelectBoxProperty(string id, string displayName, IReadOnlyList<string> options, int defaultIndex, bool isSupportKeyFrame = true, double selectBoxWidth = 120.0) : base(id, displayName, SelectBoxPropertyType.Instance, CoerceDefaultIndex(defaultIndex, options.Count), isSupportKeyFrame)
        {
            RawOptions = options;
            SelectBoxWidth = selectBoxWidth;
        }

        public SelectBoxProperty(string id, LanguageResourceKey displayNameKey, IReadOnlyList<string> options, int defaultIndex, bool isSupportKeyFrame = true, double selectBoxWidth = 120.0) : base(id, displayNameKey, SelectBoxPropertyType.Instance, CoerceDefaultIndex(defaultIndex, options.Count), isSupportKeyFrame)
        {
            RawOptions = options;
            SelectBoxWidth = selectBoxWidth;
        }

        public SelectBoxProperty(string id, string displayName, IReadOnlyList<LanguageResourceKey> optionKeys, int defaultIndex, bool isSupportKeyFrame = true, double selectBoxWidth = 120.0) : base(id, displayName, SelectBoxPropertyType.Instance, CoerceDefaultIndex(defaultIndex, optionKeys.Count), isSupportKeyFrame)
        {
            OptionKeys = optionKeys;
            SelectBoxWidth = selectBoxWidth;
        }

        public SelectBoxProperty(string id, LanguageResourceKey displayNameKey, IReadOnlyList<LanguageResourceKey> optionKeys, int defaultIndex, bool isSupportKeyFrame = true, double selectBoxWidth = 120.0) : base(id, displayNameKey, SelectBoxPropertyType.Instance, CoerceDefaultIndex(defaultIndex, optionKeys.Count), isSupportKeyFrame)
        {
            OptionKeys = optionKeys;
            SelectBoxWidth = selectBoxWidth;
        }

        static int CoerceDefaultIndex(int defaultIndex, int optionCount)
        {
            if (optionCount < 1)
            {
                throw new ArgumentException("選択肢は1つ以上必要です");
            }
            return Math.Clamp(defaultIndex, 0, optionCount - 1);
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new SelectBoxPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            var optionCount = OptionKeys?.Count ?? RawOptions!.Count;
            var index = value switch
            {
                int v => v,
                long v => (int)v,
                double v => (int)Math.Round(v),
                _ => (int)(DefaultValue ?? 0)
            };
            return Math.Clamp(index, 0, optionCount - 1);
        }
    }
}
