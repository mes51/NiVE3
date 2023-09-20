using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Property.ViewState;

namespace NiVE3.Property
{
    class Angle3DElementProperty : AngleProperty
    {
        public Angle3DElementProperty(string id, LanguageResourceKey displayNameKey, double defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayNameKey, defaultValue, isSupportKeyFrame, digit) { }

        public override PropertyViewState CreateState(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            if (layer != null)
            {
                return new DimensionDependVisibilityPropertyViewState(DisplayName, layer);
            }
            else
            {
                return base.CreateState(composition, layer, effect, viewModel);
            }
        }
    }
}
