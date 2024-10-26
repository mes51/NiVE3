using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;

namespace NiVE3.Expression.Wrapper
{
    interface IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        string name { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members

        public static IPropertyWrapper? FindProperty(PropertyGroupModel propertyGroupModel, string name, double globalTime)
        {
            foreach (var child in propertyGroupModel.Children)
            {
                if (child.Name == name)
                {
                    return Wrap(child, globalTime);
                }
            }

            return null;
        }

        public static IPropertyWrapper? FindProperty(AppendablePropertyModel appendablePropertyModel, object key, double globalTime)
        {
            if (key is string name)
            {
                foreach (var child in appendablePropertyModel.Children)
                {
                    if (child.Name == name)
                    {
                        return Wrap(child, globalTime);
                    }
                }
            }
            else if (key is int index && index > 0 && index <= appendablePropertyModel.Children.Count)
            {
                return Wrap(appendablePropertyModel.Children[index - 1], globalTime);
            }

            return null;
        }

        public static IPropertyWrapper Wrap(IPropertyModel propertyModel, double globalTime)
        {
            switch (propertyModel)
            {
                case PropertyModel p:
                    return new PropertyWrapper(p, globalTime);
                case PropertyGroupModel pg:
                    return new PropertyGroupWrapper(pg, globalTime);
                case AppendablePropertyModel ap:
                    return new AppendablePropertyWrapper(ap, globalTime);
                default:
                    throw new NotSupportedException(); // bug
            }
        }
    }

    record PropertyWrapper(PropertyModel PropertyModel, double GlobalTime) : IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => PropertyModel.Name;

        [ExpressionPublicMember]
        public int keyFrameCount => PropertyModel.KeyFrames.Count;

        [ExpressionPublicMember]
        public object? value => PropertyModel.ToExpressionValue(PropertyModel.GetValue(GlobalTime - PropertyModel.SourceStartPoint, GlobalTime));

        [ExpressionPublicMember]
        public object? valueAtTime(double time)
        {
            return PropertyModel.ToExpressionValue(PropertyModel.GetValue(time - PropertyModel.SourceStartPoint, time));
        }

        [ExpressionPublicMember]
        public KeyFrameWrapper? keyFrame(int index)
        {
            if (index < 1 || index > PropertyModel.KeyFrames.Count)
            {
                return null;
            }

            return new KeyFrameWrapper(PropertyModel, PropertyModel.KeyFrames[index - 1]);
        }

        [ExpressionPublicMember]
        public KeyFrameWrapper? getKeyFrameNextTime(double time)
        {
            foreach (var keyFrame in PropertyModel.KeyFrames)
            {
                if (keyFrame.Time >= time)
                {
                    return new KeyFrameWrapper(PropertyModel, keyFrame);
                }
            }

            return null;
        }

        [ExpressionPublicMember]
        public KeyFrameWrapper? getKeyFramePrevTime(double time)
        {
            foreach (var keyFrame in PropertyModel.KeyFrames.Reverse())
            {
                if (keyFrame.Time <= time)
                {
                    return new KeyFrameWrapper(PropertyModel, keyFrame);
                }
            }

            return null;
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }

    record PropertyGroupWrapper(PropertyGroupModel PropertyGroupModel, double GlobalTime) : IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => PropertyGroupModel.Name;

        [ExpressionPublicMember]
        public IPropertyWrapper? property(string name)
        {
            return IPropertyWrapper.FindProperty(PropertyGroupModel, name, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }

    record AppendablePropertyWrapper(AppendablePropertyModel AppendablePropertyModel, double GlobalTime) : IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => AppendablePropertyModel.Name;

        [ExpressionPublicMember]
        public IPropertyWrapper? property(object key)
        {
            return IPropertyWrapper.FindProperty(AppendablePropertyModel, key, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
