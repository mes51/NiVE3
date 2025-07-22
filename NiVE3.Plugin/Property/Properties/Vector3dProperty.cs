using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Interaction;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    public class Vector3dProperty : PropertyBase
    {
        public int Digit { get; }

        public bool Is3D { get; }

        public string DisplayUnit => UnitKey?.GetText() ?? RawUnit ?? "";

        public Vector3d MinValue { get; } = new Vector3d(double.MinValue);

        public Vector3d MaxValue { get; } = new Vector3d(double.MaxValue);

        string? RawUnit { get; }

        LanguageResourceKey? UnitKey { get; }

        string? Separator { get; }

        bool UseLinkRatio { get; }

        bool UseInteraction { get; }

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, Vector3d minValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
            MinValue = minValue;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, Vector3d minValue, Vector3d maxValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
            MinValue = minValue;
            MaxValue = maxValue;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, Vector3d minValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
            MinValue = minValue;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, Vector3d minValue, Vector3d maxValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null, string? separator = null, bool useLinkRatio = false, bool useInteraction = false) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
            MinValue = minValue;
            MaxValue = maxValue;
            Separator = separator;
            UseLinkRatio = useLinkRatio;
            UseInteraction = useInteraction;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new VectorPropertyControl
            {
                DataContext = viewModel,
                Is3D = Is3D,
                Unit = DisplayUnit,
                MinimumX = MinValue.X,
                MinimumY = MinValue.Y,
                MinimumZ = MinValue.Z,
                MaximumX = MaxValue.X,
                MaximumY = MaxValue.Y,
                MaximumZ = MaxValue.Z,
                Separator = Separator,
                UseLinkRatio = UseLinkRatio
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            if (value is Vector3d v)
            {
                if(v.IsNaN())
                {
                    return v;
                }
                else
                {
                    return Vector3d.Clamp(v, MinValue, MaxValue);
                }
            }
            else
            {
                return DefaultValue;
            }
        }

        public override PropertyInteractionBase? CreatePropertyInteraction(IPropertyInteractionViewModel viewModel)
        {
            if (UseInteraction)
            {
                return new Vector3dPropertyInteraction(viewModel, Is3D);
            }
            else
            {
                return base.CreatePropertyInteraction(viewModel);
            }
        }
    }
}
