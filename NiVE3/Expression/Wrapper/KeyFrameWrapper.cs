using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Property;

namespace NiVE3.Expression.Wrapper
{
    record KeyFrameWrapper(PropertyModel PropertyModel, KeyFrame KeyFrame)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public double time => (double)KeyFrame.Time;

        [ExpressionPublicMember]
        public object? value => PropertyModel.ToExpressionValue(KeyFrame.Value);

        [ExpressionPublicMember]
        public string interpolationType => KeyFrame.InterpolationType.ToString();

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
