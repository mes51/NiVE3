using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.ViewModel;
using NiVE3.Shared.Extension;
using Prism.Mvvm;
using NiVE3.View.Resource;
using System.Windows.Media.Animation;
using NiVE3.Data.Project;

namespace NiVE3.Model
{
    interface IPropertyModel
    {
        string Id { get; }

        object? Value { get; }

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollection<IPropertyModel>? Children { get; }

        PropertyBase Property { get; }

        event EventHandler<EventArgs>? ValueUpdated;

        PropertyControlBase CreateControl(IPropertyViewModel viewModel);

        PropertyViewState CreateState(IPropertyViewModel propertyViewModel);

        PropertyData SaveData();
    }

    partial class PropertyModel : BindableBase, IPropertyModel
    {
        const double KeyFrameEpsilon = 1E-10;

        public string Id { get; }

        private object? _value = null; // valueキーワードと被るため仕方なしでアンダーバーをつける
        public object? Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private double sourceStartPoint;
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private ObservableCollection<KeyFrame> keyFrames = new ObservableCollection<KeyFrame>();
        public ObservableCollection<KeyFrame> KeyFrames
        {
            get { return keyFrames; }
            set { SetProperty(ref keyFrames, value); }
        }

        private bool useEditingValue;
        public bool UseEditingValue
        {
            get { return useEditingValue; }
            set { SetProperty(ref useEditingValue, value); }
        }

        public ObservableCollection<IPropertyModel>? Children => null;

        public PropertyBase Property { get; }

        public event EventHandler<EventArgs>? ValueUpdated;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public PropertyModel(PropertyBase property, CompositionModel compositionModel, HistoryModel historyModel) : this(property, compositionModel, null, null, historyModel) { }

        public PropertyModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, HistoryModel historyModel) : this(property, compositionModel, layerModel, null, historyModel) { }

        public PropertyModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Id = property.Id;
            Value = property.DefaultValue;

            // NOTE: 本来はモデル側から設定してもらうものだが、引き回しの経路が複雑になりすぎる(レイヤーからだったり、エフェクトやマスクだったり)ため、自分から取りに行く
            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;
            if (layerModel != null)
            {
                layerModel.PropertyChanged += LayerModel_PropertyChanged;
            }

            PropertyChanged += PropertyModel_PropertyChanged;
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            return Property.CreateControl(CompositionModel, LayerModel, EffectModel, viewModel);
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(CompositionModel, LayerModel, EffectModel, viewModel);
        }

        public void CommitProperty(object? newValue, object? prevValue)
        {
            if (Equals(newValue, prevValue))
            {
                if (KeyFrames.Count > 0)
                {
                    CreateKeyFrame(newValue);
                }
            }
            else
            {
                if (KeyFrames.Count > 0)
                {
                    CreateKeyFrame(newValue);
                }
                else
                {
                    Value = newValue;
                    HistoryModel.Add(new ValueChangeHistoryCommand(this, prevValue, newValue));
                }
            }
        }

        public void CreateKeyFrame(object? value)
        {
            var time = CurrentTime - SourceStartPoint;
            var interpolationType = KeyFrames.LastOrDefault(k => k.Time <= time)?.InterpolationType ?? InterpolationType.Linear;
            var keyFrame = new KeyFrame(time, value, new Ease(0.0, 0.0), new Ease(0.0, 0.0), interpolationType);
            var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - time) < KeyFrameEpsilon || k.Time <= time) + 1;
            if (index > 0 && Math.Abs(KeyFrames[index - 1].Time - time) < KeyFrameEpsilon)
            {
                var oldKeyFrame = KeyFrames[index - 1];
                KeyFrames[index - 1] = keyFrame;
                HistoryModel.Add(new ReplaceSingleKeyFrameHistoryCommand(this, oldKeyFrame, keyFrame, index - 1));
            }
            else
            {
                var isFirstKeyFrame = KeyFrames.Count < 1;
                KeyFrames.Insert(index, keyFrame);
                if (isFirstKeyFrame)
                {
                    HistoryModel.Add(new AddFirstKeyFrameHistoryCommand(this, keyFrame, value));
                }
                else
                {
                    HistoryModel.Add(new AddKeyFrameHistoryCommand(this, keyFrame, index));
                }
            }
        }

        public void ClearKeyFrame()
        {
            var keyFrames = KeyFrames.ToArray();
            KeyFrames.Clear();
            HistoryModel.Add(new ClearKeyFramesHistoryCommand(this, keyFrames));
        }

        public void MoveTimeKeyFrames(KeyFrame[] targetKeyFrames, double[] newTime)
        {
            var newKeyFrames = targetKeyFrames.Zip(newTime, (k, nt) => new KeyFrame(nt, k.Value, k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)).OrderBy(k => k.Time).ToArray();
            ReplaceKeyFrames(targetKeyFrames, newKeyFrames, LanguageResourceDictionary.History_MoveKeyFrame);
        }

        public void ChangeKeyFramesInterpolationType(KeyFrame[] targetKeyFrames, InterpolationType interpolationType)
        {
            var newKeyFrames = targetKeyFrames.Select(k => new KeyFrame(k.Time, k.Value, k.EaseIn, k.EaseOut, interpolationType, k.Id)).OrderBy(k => k.Time).ToArray();
            ReplaceKeyFrames(targetKeyFrames, newKeyFrames, LanguageResourceDictionary.History_ChangeKeyFrameInterpolationType);
        }

        public void DeleteKeyFrames(KeyFrame[] targetKeyframes)
        {
            targetKeyframes = targetKeyframes.OrderBy(KeyFrames.IndexOf).ToArray();
            var oldIndices = targetKeyframes.Select(KeyFrames.IndexOf).ToArray();

            foreach (var k in targetKeyframes)
            {
                KeyFrames.Remove(k);
            }

            HistoryModel.Add(new DeleteKeyFramesHistoryCommand(this, targetKeyframes, oldIndices));
        }

        public object? GetValue(double time)
        {
            if (UseEditingValue || KeyFrames.Count < 1)
            {
                return Value;
            }
            else
            {
                return Property.PropertyType.Interpolate(KeyFrames, time);
            }
        }

        public PropertyData SaveData()
        {
            var keyFramesData = KeyFrames.Select(k =>
            {
                return new KeyFrameData
                {
                    Time = k.Time,
                    Value = Property.PropertyType.SerializeValue(k.Value),
                    EaseIn = k.EaseIn,
                    EaseOut = k.EaseOut,
                    Id = k.Id
                };
            }).ToArray();
            return new PropertyData
            {
                Id = Id,
                Value = Property.PropertyType.SerializeValue(Value),
                KeyFrames = keyFramesData
            };
        }

        void ReplaceKeyFrames(KeyFrame[] targetKeyFrames, KeyFrame[] newKeyFrames, string historyNameKey)
        {
            foreach (var k in targetKeyFrames)
            {
                KeyFrames.Remove(k);
            }

            var oldKeyFrames = new List<KeyFrame>();
            oldKeyFrames.AddRange(targetKeyFrames);
            foreach (var nk in newKeyFrames)
            {
                var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - nk.Time) < KeyFrameEpsilon || k.Time <= nk.Time) + 1;
                if (index > 0 && Math.Abs(KeyFrames[index - 1].Time - nk.Time) < KeyFrameEpsilon)
                {
                    oldKeyFrames.Add(KeyFrames[index - 1]);
                    KeyFrames[index - 1] = nk;
                }
                else
                {
                    KeyFrames.Insert(index, nk);
                }
            }

            oldKeyFrames.Sort((a, b) => a.Time.CompareTo(b.Time));
            HistoryModel.Add(new ReplaceKeyFramesHistoryCommand(this, oldKeyFrames.ToArray(), newKeyFrames, historyNameKey));
        }

        private void CompositionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompositionModel.CurrentTime))
            {
                CurrentTime = CompositionModel.CurrentTime;
            }
        }

        private void LayerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerModel.SourceStartPoint))
            {
                SourceStartPoint = LayerModel?.SourceStartPoint ?? 0.0;
            }
        }

        private void PropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value))
            {
                ValueUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    class PropertyGroupModel : BindableBase, IPropertyModel
    {
        public string Id { get; }

        public object? Value => null;

        public ObservableCollection<KeyFrame>? KeyFrames => null;

        private ObservableCollection<IPropertyModel> children = new ObservableCollection<IPropertyModel>();
        public ObservableCollection<IPropertyModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public PropertyBase Property { get; }

        public event EventHandler<EventArgs>? ValueUpdated;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, HistoryModel historyModel) : this(property, compositionModel, null, null, historyModel) { }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, HistoryModel historyModel) : this(property, compositionModel, layerModel, null, historyModel) { }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Id = property.Id;

            foreach (var c in ((PropertyGroup)property).Children)
            {
                if (c is PropertyGroup)
                {
                    Children.Add(new PropertyGroupModel(c, compositionModel, layerModel, effectModel, historyModel));
                }
                else
                {
                    Children.Add(new PropertyModel(c, compositionModel, layerModel, effectModel, historyModel));
                }
            }

            foreach (var c in Children)
            {
                c.ValueUpdated += Child_ValueUpdated;
            }
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(CompositionModel, LayerModel, EffectModel, viewModel);
        }

        public PropertyValueGroup GetPropertyValueGroup(double time)
        {
            var result = new Dictionary<string, object?>();

            foreach (var p in Children)
            {
                if (p is PropertyGroupModel pg)
                {
                    result.Add(pg.Id, pg.GetPropertyValueGroup(time));
                }
                else if (p is PropertyModel pp)
                {
                    result.Add(pp.Id, pp.GetValue(time));
                }
            }

            return new PropertyValueGroup(result);
        }

        public PropertyData SaveData()
        {
            return new PropertyData
            {
                Id = Id,
                Children = Children.Select(p => p.SaveData()).ToArray()
            };
        }

        private void Child_ValueUpdated(object? sender, EventArgs e)
        {
            ValueUpdated?.Invoke(sender, e);
        }
    }
}
