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
    /// 値を持たない、クリック操作のみのプロパティ
    /// クリックは Clicked イベントとしてプロパティを生成したエフェクトへ通知されます
    /// </summary>
    public class ButtonProperty : PropertyBase
    {
        public double MinButtonWidth { get; }

        /// <summary>
        /// ボタンがクリックされた際に発生します
        /// </summary>
        public event EventHandler<EventArgs>? Clicked;

        public ButtonProperty(string id, string displayName, double minButtonWidth = 90.0) : base(id, displayName, ButtonPropertyType.Instance, null, false)
        {
            MinButtonWidth = minButtonWidth;
        }

        public ButtonProperty(string id, LanguageResourceKey displayNameKey, double minButtonWidth = 90.0) : base(id, displayNameKey, ButtonPropertyType.Instance, null, false)
        {
            MinButtonWidth = minButtonWidth;
        }

        /// <summary>
        /// クリックを通知します
        /// </summary>
        public void PerformClick()
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new ButtonPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            return null;
        }
    }
}
