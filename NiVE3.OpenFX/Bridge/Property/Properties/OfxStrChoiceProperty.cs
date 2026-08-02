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
    /// OFX の StrChoice パラメータ用プロパティ。実行時に決まる選択肢一覧から選択し、
    /// 値は選択肢ごとに定義された列挙文字列 (string) で保存するため、
    /// プラグインの更新で選択肢の表示名や並びが変わっても保存値の互換性が保たれます。
    /// 表示順 (ChoiceOrder) の並べ替えはブリッジが構築時に適用します
    /// </summary>
    public class OfxStrChoiceProperty : PropertyBase
    {
        /// <summary>
        /// 選択肢の表示名一覧 (表示順)
        /// </summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary>
        /// 選択肢ごとの値 (列挙文字列) の一覧 (Options と同数・同順)
        /// </summary>
        public IReadOnlyList<string> OptionValues { get; }

        public double SelectBoxWidth { get; }

        public OfxStrChoiceProperty(string id, string displayName, IReadOnlyList<string> options, IReadOnlyList<string> optionValues, string defaultValue, double selectBoxWidth = 120.0) : base(id, displayName, OfxStrChoicePropertyType.Instance, CoerceDefaultValue(defaultValue, options.Count, optionValues), false)
        {
            Options = options;
            OptionValues = optionValues;
            SelectBoxWidth = selectBoxWidth;
        }

        static string CoerceDefaultValue(string defaultValue, int optionCount, IReadOnlyList<string> optionValues)
        {
            if (optionCount < 1 || optionValues.Count != optionCount)
            {
                throw new ArgumentException("選択肢は1つ以上、値は選択肢と同数必要です");
            }
            return optionValues.Contains(defaultValue) ? defaultValue : optionValues[0];
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new OfxStrChoicePropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            // 保存済みの列挙値が選択肢から消えた場合は既定値へ (スペック推奨の動作)
            return value is string s && OptionValues.Contains(s) ? s : DefaultValue;
        }
    }
}
