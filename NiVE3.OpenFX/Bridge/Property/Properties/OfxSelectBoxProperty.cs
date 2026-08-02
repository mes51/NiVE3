using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Bridge.Property.Control;
using NiVE3.OpenFX.Bridge.Property.Types;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;

namespace NiVE3.OpenFX.Bridge.Property.Properties
{
    /// <summary>
    /// 実行時に決まる選択肢一覧から選択するプロパティ。値は選択肢のインデックス (int)
    /// </summary>
    public class OfxSelectBoxProperty : PropertyBase
    {
        /// <summary>
        /// 選択肢の表示名一覧。LanguageResourceKey で構築された場合は取得時に現在の言語で解決されます
        /// </summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary>
        /// 選択肢の表示順を決める並び順キー (Options と同数)。昇順に並べ替えて表示されます。
        /// 値 (インデックス) は Options の並び基準のまま維持されるため、表示順を変えても保存値の互換性は保たれます
        /// </summary>
        public IReadOnlyList<int>? DisplayOrder { get; init; }

        /// <summary>
        /// 表示用の選択肢一覧 (DisplayOrder 適用済み)
        /// </summary>
        public IReadOnlyList<string> DisplayOptions
        {
            get
            {
                var options = Options;
                if (DisplayOrder == null || DisplayOrder.Count != options.Count)
                {
                    return options;
                }
                return [.. Enumerable.Range(0, options.Count).OrderBy(i => DisplayOrder[i]).Select(i => options[i])];
            }
        }

        public double SelectBoxWidth { get; }

        public OfxSelectBoxProperty(string id, string displayName, IReadOnlyList<string> options, int defaultIndex, bool isSupportKeyFrame = true, double selectBoxWidth = 120.0) : base(id, displayName, OfxSelectBoxPropertyType.Instance, CoerceDefaultIndex(defaultIndex, options.Count), isSupportKeyFrame)
        {
            Options = options;
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
            var control = new OfxSelectBoxPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            var optionCount = Options.Count;
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
