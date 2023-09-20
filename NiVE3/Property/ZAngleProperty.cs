using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;
using NiVE3.Property.ViewState;

namespace NiVE3.Property
{
    class ZAngleProperty : AngleProperty
    {
        LanguageResourceKey DisplayName2D { get; }

        LanguageResourceKey DisplayName3D { get; }

        public ZAngleProperty(string id, LanguageResourceKey displayNameKey2D, LanguageResourceKey displayNameKey3D, double defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayNameKey2D, defaultValue, isSupportKeyFrame, digit)
        {
            DisplayName2D = displayNameKey2D;
            DisplayName3D = displayNameKey3D;
        }

        public override PropertyViewState CreateState(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            if (layer != null)
            {
                return new DimensionDependNamePropertyViewState(DisplayName2D, DisplayName3D, layer);
            }
            else
            {
                return base.CreateState(composition, layer, effect, viewModel);
            }
        }
    }
}
