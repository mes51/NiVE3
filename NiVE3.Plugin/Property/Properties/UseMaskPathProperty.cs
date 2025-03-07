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
    public class UseMaskPathProperty : LayerDependPropertyBase
    {
        public double SelectBoxWidth { get; }

        public UseMaskPathProperty(string id, string displayName, double selectBoxWidth = 75.0) : base(id, displayName, UseMaskPathPropertyType.Instance, UseMaskPathTarget.Empty)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public UseMaskPathProperty(string id, LanguageResourceKey displayNameKey, double selectBoxWidth = 75.0) : base(id, displayNameKey, UseMaskPathPropertyType.Instance, UseMaskPathTarget.Empty)
        {
            SelectBoxWidth = selectBoxWidth;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            if (layer == null)
            {
                // TODO: 本体側でのハンドリング or コンポジションプロパティの取りやめ
                throw new NotSupportedException();
            }

            var control = new UseMaskPathPropertyControl(layer)
            {
                DataContext = viewModel,
                MaskCollectionSource = layer.MaskViewModels
            };
            return control;
        }

        public override object? ChangeValueByLayerStateChanged(object? value, ILayerObject layer)
        {
            if (value is UseMaskPathTarget target)
            {
                if (layer.MaskIdentifiers.Any(id => target.MaskId == id))
                {
                    return target;
                }
            }

            return UseMaskPathTarget.Empty;
        }

        public override object? ChangeValueByReplaceEffectId(object? value, Dictionary<Guid, Guid> effectIdMap, ILayerObject layer)
        {
            return value;
        }

        public override object? ChangeValueByReplaceMaskId(object? value, Dictionary<Guid, Guid> maskIdMap, ILayerObject layer)
        {
            if (value is UseMaskPathTarget target)
            {
                if (maskIdMap.TryGetValue(target.MaskId, out var newMaskId))
                {
                    return new UseMaskPathTarget(newMaskId);
                }
                else
                {
                    return target;
                }
            }

            return UseMaskPathTarget.Empty;
        }

        public override bool ValidateValue(object? value, ILayerObject layer)
        {
            if (value is UseMaskPathTarget target)
            {
                return target == UseMaskPathTarget.Empty || layer.MaskIdentifiers.Any(id => target.MaskId == id);
            }
            else
            {
                return false;
            }
        }

        public override object? CoerceValue(object? value, ILayerObject layer)
        {
            if (value is UseMaskPathTarget target)
            {
                if (layer.MaskIdentifiers.Any(id => target.MaskId == id))
                {
                    return target;
                }
            }

            return UseMaskPathTarget.Empty;
        }
    }
}
