using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Input;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    class LayerTextPropertyWrapper
    {
        public LayerTextPropertyWrapper(PropertyGroupModel textProperty, Time globalTime)
        {
            if (textProperty.FindProperty(TextFootageSource.SourceTextId) is IPropertyModel sourceTextProperty)
            {
                sourceText = IPropertyWrapper.Wrap(sourceTextProperty, globalTime);
            }
            if (textProperty.FindProperty(TextFootageSource.TextMoreOptionsGroupId) is IPropertyModel optionsProperty)
            {
                options = IPropertyWrapper.Wrap(optionsProperty, globalTime);
            }
            if (textProperty.FindProperty(TextFootageSource.TextAnimatorsId) is IPropertyModel animatorsProperty)
            {
                animators = IPropertyWrapper.Wrap(animatorsProperty, globalTime);
            }
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public IPropertyWrapper? sourceText { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? options { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? animators { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
