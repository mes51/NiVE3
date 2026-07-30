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
    /// 文字列を入力するプロパティ
    /// </summary>
    public class StringProperty : PropertyBase
    {
        /// <summary>
        /// 読み取り専用 (表示のみ) かどうか
        /// </summary>
        public bool IsReadOnly { get; }

        public double TextBoxWidth { get; }

        /// <summary>
        /// 複数行の入力を受け付けるかどうか。
        /// 複数行の場合、Enter は改行の挿入になり、確定はフォーカス喪失または Ctrl+Enter で行います
        /// </summary>
        public bool IsMultiLine { get; }

        public StringProperty(string id, string displayName, string defaultValue, bool isReadOnly = false, double textBoxWidth = 200.0, bool isMultiLine = false) : base(id, displayName, StringPropertyType.Instance, defaultValue, false)
        {
            IsReadOnly = isReadOnly;
            TextBoxWidth = textBoxWidth;
            IsMultiLine = isMultiLine;
        }

        public StringProperty(string id, LanguageResourceKey displayNameKey, string defaultValue, bool isReadOnly = false, double textBoxWidth = 200.0, bool isMultiLine = false) : base(id, displayNameKey, StringPropertyType.Instance, defaultValue, false)
        {
            IsReadOnly = isReadOnly;
            TextBoxWidth = textBoxWidth;
            IsMultiLine = isMultiLine;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new StringPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            return value as string ?? DefaultValue;
        }
    }
}
