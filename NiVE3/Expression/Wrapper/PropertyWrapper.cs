using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;

namespace NiVE3.Expression.Wrapper
{
    interface IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        string name { get; }

        IPropertyWrapper? parentProperty { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members

        public static IPropertyWrapper? FindProperty(PropertyGroupModel propertyGroupModel, object key, Time globalTime)
        {
            if (key is string name)
            {
                foreach (var child in propertyGroupModel.Children)
                {
                    if (child.Name == name)
                    {
                        return Wrap(child, globalTime);
                    }
                }
            }
            else if (ExpressionInternalUtil.TryConvertToIndex(key, out var index) && index > -1 && index < propertyGroupModel.Children.Count)
            {
                return Wrap(propertyGroupModel.Children[index], globalTime);
            }

            return null;
        }

        public static IPropertyWrapper? FindProperty(AppendablePropertyModel appendablePropertyModel, object key, Time globalTime)
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
            else if (ExpressionInternalUtil.TryConvertToIndex(key, out var index) && index > -1 && index < appendablePropertyModel.Children.Count)
            {
                return Wrap(appendablePropertyModel.Children[index], globalTime);
            }

            return null;
        }

        public static IPropertyWrapper Wrap(IPropertyModel propertyModel, Time globalTime)
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

    record PropertyWrapper(PropertyModel PropertyModel, Time GlobalTime) : IPropertyWrapper
    {
        const string LoopModeCycle = "cycle";

        const string LoopModePingPong = "pingpong";

        const string LoopDirectionInfinity = "infinity";

        const string LoopDirectionIn = "in";

        const string LoopDirectionOut = "out";

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => PropertyModel.Name;

        [ExpressionPublicMember]
        public IPropertyWrapper? parentProperty => IPropertyWrapper.Wrap(PropertyModel.ParentPropertyModel, GlobalTime);

        [ExpressionPublicMember]
        public int keyFrameCount => PropertyModel.KeyFrames.Count;

        [ExpressionPublicMember]
        public object? value => PropertyModel.ToExpressionValue(PropertyModel.GetValue(GlobalTime - PropertyModel.SourceStartPoint, GlobalTime));

        [ExpressionPublicMember]
        public object? valueAtTime(double time)
        {
            return PropertyModel.ToExpressionValue(PropertyModel.GetValue(time - PropertyModel.SourceStartPoint, (Time)time));
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
            var layerTime = time - PropertyModel.SourceStartPoint;
            foreach (var keyFrame in PropertyModel.KeyFrames)
            {
                if (keyFrame.Time >= layerTime)
                {
                    return new KeyFrameWrapper(PropertyModel, keyFrame);
                }
            }

            return null;
        }

        [ExpressionPublicMember]
        public KeyFrameWrapper? getKeyFramePrevTime(double time)
        {
            var layerTime = time - PropertyModel.SourceStartPoint;
            foreach (var keyFrame in PropertyModel.KeyFrames.Reverse())
            {
                if (keyFrame.Time <= layerTime)
                {
                    return new KeyFrameWrapper(PropertyModel, keyFrame);
                }
            }

            return null;
        }

        [ExpressionPublicMember]
        public object? loop()
        {
            return loop(LoopModeCycle, LoopDirectionInfinity, 0, PropertyModel.KeyFrames.Count);
        }

        [ExpressionPublicMember]
        public object? loop(string mode)
        {
            return loop(mode, LoopDirectionInfinity, 0, PropertyModel.KeyFrames.Count);
        }

        [ExpressionPublicMember]
        public object? loop(string mode, string direction)
        {
            return loop(mode, direction, 0, PropertyModel.KeyFrames.Count);
        }

        [ExpressionPublicMember]
        public object? loop(string mode, string direction, int rangeStart)
        {
            return loop(mode, direction, rangeStart, PropertyModel.KeyFrames.Count);
        }

        [ExpressionPublicMember]
        public object? loop(string mode, string direction, int rangeBegin, int rangeEnd)
        {
            if (mode != LoopModeCycle && mode != LoopModePingPong)
            {
                throw new ArgumentException(null, nameof(mode));
            }
            if (direction != LoopDirectionInfinity && direction != LoopDirectionIn &&  direction != LoopDirectionOut)
            {
                throw new ArgumentException(null, nameof(direction));
            }
            if (PropertyModel.KeyFrames.Count < 0)
            {
                throw new InvalidOperationException("keyframe not found");
            }

            if (PropertyModel.KeyFrames.Count < 1)
            {
                return PropertyModel.GetValue(GlobalTime - PropertyModel.SourceStartPoint, GlobalTime);
            }

            if (rangeEnd < rangeBegin)
            {
                (rangeBegin, rangeEnd) = (rangeEnd, rangeBegin);
            }
            rangeBegin = Math.Max(rangeBegin, 1) - 1;
            rangeEnd = Math.Min(rangeEnd, PropertyModel.KeyFrames.Count) - 1;

            var beginKeyFrame = PropertyModel.KeyFrames[rangeBegin];
            var endKeyFrame = PropertyModel.KeyFrames[rangeEnd];
            var timeRange = endKeyFrame.Time - beginKeyFrame.Time;

            var targetTime = Time.Zero;
            var baseTime = GlobalTime - PropertyModel.SourceStartPoint - beginKeyFrame.Time;
            switch (mode)
            {
                case LoopModeCycle:
                    targetTime = (((baseTime % timeRange) + timeRange) % timeRange) + beginKeyFrame.Time;
                    break;
                case LoopModePingPong:
                    {
                        var b = Time.Abs(baseTime / timeRange) % 2.0;
                        targetTime = (b - Time.Max(b - 1.0, Time.Zero) * 2.0) * timeRange + beginKeyFrame.Time;
                    }
                    break;
            }

            switch (direction)
            {
                case LoopDirectionIn:
                    if (baseTime >= 0.0)
                    {
                        targetTime = GlobalTime - PropertyModel.SourceStartPoint;
                    }
                    break;
                case LoopDirectionOut:
                    if (baseTime + beginKeyFrame.Time < endKeyFrame.Time)
                    {
                        targetTime = GlobalTime - PropertyModel.SourceStartPoint;
                    }
                    break;
            }

            return PropertyModel.ToExpressionValue(PropertyModel.GetValue(targetTime, targetTime + PropertyModel.SourceStartPoint));
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }

    record PropertyGroupWrapper(PropertyGroupModel PropertyGroupModel, Time GlobalTime) : IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => PropertyGroupModel.Name;

        [ExpressionPublicMember]
        public IPropertyWrapper? parentProperty => PropertyGroupModel.ParentPropertyModel != null ? IPropertyWrapper.Wrap(PropertyGroupModel.ParentPropertyModel, GlobalTime) : null;

        [ExpressionPublicMember]
        public IPropertyWrapper? property(object key)
        {
            return IPropertyWrapper.FindProperty(PropertyGroupModel, key, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }

    record AppendablePropertyWrapper(AppendablePropertyModel AppendablePropertyModel, Time GlobalTime) : IPropertyWrapper
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => AppendablePropertyModel.Name;

        [ExpressionPublicMember]
        public IPropertyWrapper? parentProperty => IPropertyWrapper.Wrap(AppendablePropertyModel.ParentPropertyModel, GlobalTime);

        [ExpressionPublicMember]
        public IPropertyWrapper? property(object key)
        {
            return IPropertyWrapper.FindProperty(AppendablePropertyModel, key, GlobalTime);
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
