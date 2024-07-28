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
    public class EnumProperty : PropertyBase
    {
        public Type EnumType { get; }

        public Type EnumNameLanguageResourceDictionaryType { get; }

        public double SelectBoxWidth { get; }

        object[] Values { get; }

        public EnumProperty(string id, string displayName, Type enumType, Type enumNameLanguageResourceDictionaryType, object defaultValue, bool isSupportKeyFrame = true, double selectBoxWidth = 75.0) : base(id, displayName, EnumPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException(nameof(EnumType));
            }

            EnumType = enumType;
            EnumNameLanguageResourceDictionaryType = enumNameLanguageResourceDictionaryType;
            Values = Enum.GetValues(enumType).Cast<object>().ToArray();
            SelectBoxWidth = selectBoxWidth;
        }

        public EnumProperty(string id, LanguageResourceKey displayNameKey, Type enumType, Type enumNameLanguageResourceDictionaryType, object? defaultValue, bool isSupportKeyFrame = true, double selectBoxWidth = 75.0) : base(id, displayNameKey, EnumPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException(nameof(EnumType));
            }

            EnumType = enumType;
            EnumNameLanguageResourceDictionaryType = enumNameLanguageResourceDictionaryType;
            Values = Enum.GetValues(enumType).Cast<object>().ToArray();
            SelectBoxWidth = selectBoxWidth;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new EnumPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            if (value != null && Values.Contains(value))
            {
                return value;
            }
            else
            {
                return Values.First();
            }
        }

        public override bool ValidateValue(object? value)
        {
            return value?.GetType() == EnumType;
        }
    }
}
