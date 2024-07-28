using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property.Properties
{
    public class UseLayerImageProperty : CompositionDependPropertyBase
    {
        public double SelectBoxWidth { get; }

        public UseLayerImageProperty(string id, string displayName, double selectBoxWidth = 75.0) : base(id, displayName, UseLayerImagePropertyType.Instance, UseLayerImageTarget.Empty)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public UseLayerImageProperty(string id, LanguageResourceKey displayNameKey, double selectBoxWidth = 75.0) : base(id, displayNameKey, UseLayerImagePropertyType.Instance, UseLayerImageTarget.Empty)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new UseLayerImagePropertyControl(composition)
            {
                DataContext = viewModel,
                LayerCollectionSource = composition.LayerViewModels
            };
            return control;
        }

        public override object? ChangeValueByCompositionStateChanged(object? value, ICompositionObject composition)
        {
            if (value is UseLayerImageTarget target)
            {
                if (composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage))
                {
                    return target;
                }
            }

            return UseLayerImageTarget.Empty;
        }

        public override bool ValidateValue(object? value, ICompositionObject composition)
        {
            if (value is UseLayerImageTarget target)
            {
                return target == UseLayerImageTarget.Empty || composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage);
            }
            else
            {
                return false;
            }
        }

        public override object? CoerceValue(object? value, ICompositionObject composition)
        {
            if (value is UseLayerImageTarget target)
            {
                if (composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage))
                {
                    return target;
                }
            }

            return UseLayerImageTarget.Empty;
        }
    }
}
