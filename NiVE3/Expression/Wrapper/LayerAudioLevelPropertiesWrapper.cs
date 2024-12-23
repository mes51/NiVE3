using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    class LayerAudioLevelPropertiesWrapper
    {
        public LayerAudioLevelPropertiesWrapper(PropertyGroupModel levels, Time globalTime)
        {
            if (levels.FindProperty(ILayerObject.AudioLevelId) is PropertyModel levelProperty)
            {
                level = new PropertyWrapper(levelProperty, globalTime);
            }
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public IPropertyWrapper? level { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
