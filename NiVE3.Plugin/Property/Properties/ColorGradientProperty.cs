using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Color;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    public class ColorGradientProperty : PropertyBase
    {
        string EditButtonText { get; }

        string OKButtonText { get; }

        string CancelButtonText { get; }

        bool ShowPreviewOKLabInterpolation { get; }

        public ColorGradientProperty(string id, string displayName, string editButtonText, string okButtonText, string cancelButtonText, ColorGradient? defaultValue = null, bool isSupportKeyFrame = true, bool showPreviewOKLabInterpolation = false) :
            base(id, displayName, ColorGradientPropertyType.Instance, defaultValue ?? ColorGradient.WhiteBlackGradient, isSupportKeyFrame)
        {
            EditButtonText = editButtonText;
            OKButtonText = okButtonText;
            CancelButtonText = cancelButtonText;
            ShowPreviewOKLabInterpolation = showPreviewOKLabInterpolation;
        }

        public ColorGradientProperty(string id, LanguageResourceKey displayNameKey, LanguageResourceKey editButtonTextKey, LanguageResourceKey okButtonTextKey, LanguageResourceKey cancelButtonTextKey, ColorGradient? defaultValue = null, bool isSupportKeyFrame = true, bool showPreviewOKLabInterpolation = false) :
            base(id, displayNameKey, ColorGradientPropertyType.Instance, defaultValue ?? ColorGradient.WhiteBlackGradient, isSupportKeyFrame)
        {
            EditButtonText = editButtonTextKey.GetText() ?? "";
            OKButtonText = okButtonTextKey.GetText() ?? "";
            CancelButtonText = cancelButtonTextKey.GetText() ?? "";
            ShowPreviewOKLabInterpolation = showPreviewOKLabInterpolation;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new ColorGradientPropertyControl
            {
                EditButtonText = EditButtonText,
                DialogOKButtonText = OKButtonText,
                DialogCancelButtonText = CancelButtonText,
                ShowPreviewOKLabInterpolation = ShowPreviewOKLabInterpolation
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            return (value as ColorGradient) ?? ColorGradient.Empty;
        }
    }
}
