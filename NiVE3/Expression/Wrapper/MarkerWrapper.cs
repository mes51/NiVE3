using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    record MarkerWrapper(Marker Marker)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public double time => (double)Marker.Time;

        [ExpressionPublicMember]
        public string name => Marker.Name;

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
