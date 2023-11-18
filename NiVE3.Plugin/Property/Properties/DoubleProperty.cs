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
    public class DoubleProperty : PropertyBase
    {
        public double MinValue { get; }

        public double MaxValue { get; }

        public double SlideChangeValue { get; }

        public int Digit { get; }

        public string Unit { get; }

        public DoubleProperty(string id, string displayName, double defaultValue, double minValue, double maxValue, bool isSupportKeyFrame = true, double slideChangeValue = 1.0, int digit = -1, string unit = "") : base(id, displayName, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException(nameof(minValue));
            }

            MinValue = minValue;
            MaxValue = maxValue;
            SlideChangeValue = slideChangeValue;
            Digit = digit;
            Unit = unit;
        }

        public DoubleProperty(string id, LanguageResourceKey displayNameKey, double defaultValue, double minValue, double maxValue, bool isSupportKeyFrame = true, double slideChangeValue = 1.0, int digit = -1, LanguageResourceKey? unitKey = null) : base(id, displayNameKey, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException(nameof(minValue));
            }

            MinValue = minValue;
            MaxValue = maxValue;
            SlideChangeValue = slideChangeValue;
            Digit = digit;
            Unit = unitKey?.GetText() ?? "";
        }

        public override object CoerceValue(object value)
        {
            return Math.Clamp((double)value, MinValue, MaxValue);
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new DoublePropertyControl();
            control.DataContext = viewModel;
            control.Unit = Unit;
            return control;
        }

        public override bool ValidateValue(object value)
        {
            return value is double;
        }
    }
}
