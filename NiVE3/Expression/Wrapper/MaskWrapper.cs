using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    record MaskWrapper(MaskModel MaskModel, Time GlobalTime)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => MaskModel.Name;

        [ExpressionPublicMember]
        public bool isEnable => MaskModel.IsEnable;

        [ExpressionPublicMember]
        public IPropertyWrapper? property(object key)
        {
            return IPropertyWrapper.FindProperty(MaskModel.Properties, key, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
