using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;

namespace NiVE3.Expression.Wrapper
{
    record EffectWrapper(EffectModel EffectModel, double GlobalTime)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => EffectModel.Name;

        [ExpressionPublicMember]
        public string comment => EffectModel.Comment;

        [ExpressionPublicMember]
        public bool isEnable => EffectModel.IsEnable;

        [ExpressionPublicMember]
        public IPropertyWrapper? property(object key)
        {
            return IPropertyWrapper.FindProperty(EffectModel.Properties, key, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
