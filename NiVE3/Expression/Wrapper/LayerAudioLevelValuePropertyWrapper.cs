using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    class LayerAudioLevelValuePropertyWrapper
    {
        public LayerAudioLevelValuePropertyWrapper(PropertyGroupModel audioLevelValues, Time globalTime)
        {
            if (audioLevelValues.FindProperty(LayerModel.AudioLevelValueLeftId) is PropertyModel leftProperty)
            {
                left = new PropertyWrapper(leftProperty, globalTime);
            }
            if (audioLevelValues.FindProperty(LayerModel.AudioLevelValueRightId) is PropertyModel rightProperty)
            {
                right = new PropertyWrapper(rightProperty, globalTime);
            }
            if (audioLevelValues.FindProperty(LayerModel.AudioLevelValueBothId) is PropertyModel bothProperty)
            {
                both = new PropertyWrapper(bothProperty, globalTime);
            }
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public IPropertyWrapper? left { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? right { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? both { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
