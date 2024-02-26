using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
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

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
        }

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, Vector3d minValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
            MinValue = minValue;
        }

        public Vector3dProperty(string id, string displayName, Vector3d defaultValue, Vector3d minValue, Vector3d maxValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, string? unit = null) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            RawUnit = unit;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, Vector3d minValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
            MinValue = minValue;
        }

        public Vector3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, Vector3d minValue, Vector3d maxValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false, LanguageResourceKey? unitKey = null) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
            UnitKey = unitKey;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new VectorPropertyControl();
            control.DataContext = viewModel;
            control.Is3D = Is3D;
            control.Unit = DisplayUnit;
            control.MinimumX = MinValue.X;
            control.MinimumY = MinValue.Y;
            control.MinimumZ = MinValue.Z;
            control.MaximumX = MaxValue.X;
            control.MaximumY = MaxValue.Y;
            control.MaximumZ = MaxValue.Z;
            return control;
        }

        public override object CoerceValue(object value)
        {
            return value;
        }

        public override bool ValidateValue(object value)
        {
            return value is Vector3d;
        }
    }
}
