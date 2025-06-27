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
    public class UseLayerAudioProperty : CompositionDependPropertyBase
    {
        public double SelectBoxWidth { get; }

        public UseLayerAudioProperty(string id, string displayName, double selectBoxWidth = 75.0) : base(id, displayName, UseLayerAudioPropertyType.Instance, null)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public UseLayerAudioProperty(string id, LanguageResourceKey displayNameKey, double selectBoxWidth = 75.0) : base(id, displayNameKey, UseLayerAudioPropertyType.Instance, null)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            return new UseLayerAudioPropertyControl(composition)
            {
                DataContext = viewModel,
                LayerCollectionSource = composition.LayerViewModels
            };
        }

        public override object? ChangeValueByCompositionStateChanged(object? value, ICompositionObject composition)
        {
            if (value is UseLayerAudioTarget target)
            {
                if (composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage))
                {
                    return target;
                }
            }

            return UseLayerAudioTarget.Empty;
        }

        public override object? ChangeValueByReplaceLayerId(object? value, Dictionary<Guid, Guid> layerIdMap, ICompositionObject composition)
        {
            if (value is UseLayerAudioTarget target)
            {
                if (layerIdMap.TryGetValue(target.LayerId, out var newLayerId))
                {
                    return new UseLayerAudioTarget(newLayerId, target.AudioProcessType);
                }
                else
                {
                    return target;
                }
            }

            return UseLayerAudioTarget.Empty;
        }

        public override bool ValidateValue(object? value, ICompositionObject composition)
        {
            if (value is UseLayerAudioTarget target)
            {
                return target == UseLayerAudioTarget.Empty || composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage);
            }
            else
            {
                return false;
            }
        }

        public override object? CoerceValue(object? value, ICompositionObject composition)
        {
            if (value is UseLayerAudioTarget target)
            {
                if (composition.LayerIdentifiers.Any(l => l.LayerId == target.LayerId && l.HasImage))
                {
                    return target;
                }
            }

            return UseLayerAudioTarget.Empty;
        }
    }
}
