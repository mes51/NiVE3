using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    public class ColorProperty : PropertyBase
    {
        string DialogTitle { get; }

        string OKButtonText { get; }

        string CancelButtonText { get; }

        public ColorProperty(string id, string displayName, string dialogTitle, string okButtonText, string cancelButtonText, Vector4 defaultValue, bool isSupportKeyFrame = true) : base(id, displayName, ColorPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            DialogTitle = dialogTitle;
            OKButtonText = okButtonText;
            CancelButtonText = cancelButtonText;
        }

        public ColorProperty(string id, LanguageResourceKey displayNameKey, LanguageResourceKey dialogTitleKey, LanguageResourceKey okButtonTextKey, LanguageResourceKey cancelButtonTextKey, Vector4 defaultValue, bool isSupportKeyFrame = true) : base(id, displayNameKey, ColorPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            DialogTitle = dialogTitleKey.GetText() ?? "";
            OKButtonText = okButtonTextKey.GetText() ?? "";
            CancelButtonText = cancelButtonTextKey.GetText() ?? "";
        }

        public override object CoerceValue(object value)
        {
            var color = value switch
            {
                Vector4 v => v,
                Color c => new Vector4(c.B, c.G, c.R, c.A) / 255.0F,
                _ => new Vector4()
            };
            return color;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new ColorPropertyControl
            {
                DataContext = viewModel,
                DialogTitle = DialogTitle,
                OKButtonText = OKButtonText,
                CancelButtonText = CancelButtonText
            };
            return control;
        }

        public override bool ValidateValue(object value)
        {
            return value is Vector4 || value is Color;
        }
    }
}
