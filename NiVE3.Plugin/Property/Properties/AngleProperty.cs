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
    public class AngleProperty : PropertyBase
    {
        public int Digit { get; }

        public bool IsOnlyPositiveDirection { get; }

        public AngleProperty(string id, string displayName, double defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool isOnlyPositiveDirection = false) : base(id, displayName, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            IsOnlyPositiveDirection = isOnlyPositiveDirection;
        }

        public AngleProperty(string id, LanguageResourceKey displayNameKey, double defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool isOnlyPositiveDirection = false) : base(id, displayNameKey, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            IsOnlyPositiveDirection = isOnlyPositiveDirection;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new AnglePropertyControl
            {
                DataContext = viewModel,
                IsOnlyPositiveDirection = IsOnlyPositiveDirection
            };
            return control;
        }

        public override object CoerceValue(object? value)
        {
            if (value is double angle)
            {
                return IsOnlyPositiveDirection ? Math.Max(angle, 0.0) : angle;
            }
            else
            {
                return 0.0;
            }
        }

        public override bool ValidateValue(object? value)
        {
            return value is double;
        }
    }
}
