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
using NiVE3.Shared.Extension;
using Prism.Mvvm;
using NiVE3.View.Resource;
using NiVE3.Data.Json.Project;
using NiVE3.Util;
using NiVE3.Data.Clipboard;
using NiVE3.Plugin.Property.Types;
using System.IO.Hashing;
using NiVE3.Extension;

namespace NiVE3.Model
{
    partial class PropertyModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        public string Name { get; }

        private object? _value = null; // valueキーワードと被るため仕方なしでアンダーバーをつける
        public object? Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public bool IsEnable => true;

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

        private ObservableCollection<KeyFrame> keyFrames = [];
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

        public Int128 ObjectId { get; }

        public string Id => Property.Id;

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public PropertyModel(PropertyBase property, Int128 parentObjectId, CompositionModel compositionModel, HistoryModel historyModel) : this(property, parentObjectId, compositionModel, null, null, historyModel) { }

        public PropertyModel(PropertyBase property, Int128 parentObjectId, CompositionModel compositionModel, LayerModel? layerModel, HistoryModel historyModel) : this(property, parentObjectId, compositionModel, layerModel, null, historyModel) { }

        public PropertyModel(PropertyBase property, Int128 parentObjectId, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            Value = property.DefaultValue;
            SourceStartPoint = layerModel?.SourceStartPoint ?? 0.0;
            CurrentTime = compositionModel.CurrentTime;

            var objectIdHash = new XxHash3();
            objectIdHash.Append(parentObjectId);
            objectIdHash.Append(property.Id);
            ObjectId = objectIdHash.ToInt128();

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
            return Property.CreateControl(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
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
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void CreateKeyFrame(object? value)
        {
            var time = CurrentTime - SourceStartPoint;
            var interpolationType = KeyFrames.LastOrDefault(k => k.Time <= time)?.InterpolationType ?? InterpolationType.Linear;
            var keyFrame = new KeyFrame(TimeCalc.RoundTimeDigit(time), value, new Ease(0.0, 0.0), new Ease(0.0, 0.0), interpolationType);
            var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - time) < TimeCalc.TimeEpsilon || k.Time <= time) + 1;
            if (index > 0 && Math.Abs(KeyFrames[index - 1].Time - time) < TimeCalc.TimeEpsilon)
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
            ValueCommited?.Invoke(this, EventArgs.Empty);
        }

        public void ClearKeyFrame()
        {
            var keyFrames = KeyFrames.ToArray();
            KeyFrames.Clear();
            HistoryModel.Add(new ClearKeyFramesHistoryCommand(this, keyFrames));
            ValueCommited?.Invoke(this, EventArgs.Empty);
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
            DeleteKeyFramesInternal(targetKeyframes, false);
        }

        public IReadOnlyCollection<IPropertyObject>? GetChildren()
        {
            return Children;
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

        public object? GetCurrentTimeValue()
        {
            var time = CurrentTime - SourceStartPoint;
            return GetValue(time);
        }

        public void ResetProperty()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ResetPropertyValue));

            if (HasKeyFrames())
            {
                CreateKeyFrame(Property.DefaultValue);
            }
            else
            {
                CommitProperty(Property.DefaultValue, Value);
            }

            HistoryModel.EndGroup();
        }

        public PropertyValueGroup? GetValues(double time, bool withoutDisableProperty = false)
        {
            return null;
        }

        public void UpdateValueByCompositionStateChanged()
        {
            if (Property is CompositionDependPropertyBase cp)
            {
                var oldValue = Value;
                Value = cp.ChangeValueByCompositionStateChanged(Value, CompositionModel);

                if (oldValue != Value)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByCompositionStateChangedHistoryCommand(this, oldValue, Value));
                }
            }
        }

        public bool HasCompositionDependProperty()
        {
            return Property is CompositionDependPropertyBase;
        }

        public bool HasKeyFrames()
        {
            return KeyFrames.Count > 0;
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
                    InterpolationType = k.InterpolationType,
                    Id = k.Id
                };
            }).ToArray();
            return new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Value = Property.PropertyType.SerializeValue(Value),
                KeyFrames = keyFramesData
            };
        }

        public void LoadData(PropertyData data)
        {
            Value = Property.PropertyType.DeserializeValue(data.Value);
            if (data.KeyFrames == null)
            {
                return;
            }

            KeyFrames.Clear();
            foreach (var k in data.KeyFrames.Select(k => new KeyFrame(k.Time, Property.PropertyType.DeserializeValue(k.Value), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
            {
                KeyFrames.Add(k);
            }
        }

        public void CoerceValues()
        {
            var oldKeyFrames = KeyFrames.ToArray();
            KeyFrames.Clear();

            var cp = Property as CompositionDependPropertyBase;
            if (cp != null)
            {
                Value = cp.CoerceValue(Value, CompositionModel);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, cp.CoerceValue(cp.PropertyType.DeserializeValue(k.Value), CompositionModel), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
            else
            {
                Value = Property.CoerceValue(Value);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, Property.CoerceValue(Property.PropertyType.DeserializeValue(k.Value)), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
        }

        public void PasteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var keyFrames = data.KeyFrames ?? [];
            if (keyFrames.Length < 1)
            {
                var newValue = Property switch
                {
                    CompositionDependPropertyBase cp => cp.CoerceValue(Property.PropertyType.DeserializeValue(data.Value), CompositionModel),
                    _ => Property.CoerceValue(Property.PropertyType.DeserializeValue(data.Value))
                };
                var oldValue = Value;
                Value = newValue;

                HistoryModel.Add(new ValueChangeHistoryCommand(this, oldValue, newValue));
            }
            else
            {
                var startTime = keyFrames[0].Time;
                var newKeyFrames = new List<KeyFrame>();
                if (Property is CompositionDependPropertyBase cp)
                {
                    foreach (var k in keyFrames)
                    {
                        var newTime = TimeCalc.RoundTimeDigit(k.Time - startTime + CurrentTime - SourceStartPoint);
                        newKeyFrames.Add(new KeyFrame(newTime, cp.CoerceValue(cp.PropertyType.DeserializeValue(k.Value), CompositionModel), k.EaseIn, k.EaseOut, k.InterpolationType));
                    }
                }
                else
                {
                    foreach (var k in keyFrames)
                    {
                        var newTime = TimeCalc.RoundTimeDigit(k.Time - startTime + CurrentTime - SourceStartPoint);
                        newKeyFrames.Add(new KeyFrame(newTime, Property.CoerceValue(Property.PropertyType.DeserializeValue(k.Value)), k.EaseIn, k.EaseOut, k.InterpolationType));
                    }
                }

                var oldKeyFrames = KeyFrames.Where(k => newKeyFrames.Any(nk => nk.Time == k.Time)).ToArray();
                var oldKeyFrameIndices = KeyFrames.Where(oldKeyFrames.Contains).Select(KeyFrames.IndexOf).ToArray();
                foreach (var ok in oldKeyFrames)
                {
                    KeyFrames.Remove(ok);
                }

                var newKeyFrameIndices = new int[newKeyFrames.Count];
                foreach (var nk in newKeyFrames)
                {
                    var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - nk.Time) < TimeCalc.TimeEpsilon || k.Time <= nk.Time) + 1;
                    KeyFrames.Insert(index, nk);
                }

                HistoryModel.Add(new PasteKeyFramesHistoryCommand(this, oldKeyFrames, oldKeyFrameIndices, [.. newKeyFrames], newKeyFrameIndices));
            }
        }

        public void OverwriteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var oldKeyFrames = KeyFrames.ToArray();
            var oldValue = Value;

            LoadData(data);

            HistoryModel.Add(new OverwritePropertyHistoryCommand(this, oldKeyFrames, oldValue, [..KeyFrames], Value));
        }

        public CopyData<PropertyData> CopyProperty()
        {
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Value = Property.PropertyType.SerializeValue(Value)
            };
            return new CopyData<PropertyData>(CopyDataType.Property, [data]);
        }

        public void PasteProperty(CopyData<PropertyData> data)
        {
            var propertyData = data.Data.FirstOrDefault();
            if (propertyData == null)
            {
                return;
            }

            PasteProperty(propertyData);
        }

        public CopyData<PropertyData> CutKeyFrames(KeyFrame[] targetKeyFrames)
        {
            var result = CopyKeyFrames(targetKeyFrames);
            DeleteKeyFramesInternal(targetKeyFrames, true);

            return result;
        }

        public CopyData<PropertyData> CopyKeyFrames(KeyFrame[] targetKeyFrames)
        {
            var keyFramesData = targetKeyFrames.OrderBy(KeyFrames.IndexOf).Select(k =>
            {
                return new KeyFrameData
                {
                    Time = k.Time,
                    Value = Property.PropertyType.SerializeValue(k.Value),
                    EaseIn = k.EaseIn,
                    EaseOut = k.EaseOut,
                    InterpolationType = k.InterpolationType,
                    Id = k.Id
                };
            }).ToArray();
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Value = Property.PropertyType.SerializeValue(Value),
                KeyFrames = keyFramesData
            };

            return new CopyData<PropertyData>(CopyDataType.KeyFrame, [data]);
        }

        void DeleteKeyFramesInternal(KeyFrame[] targetKeyframes, bool isCut)
        {
            targetKeyframes = [.. targetKeyframes.OrderBy(KeyFrames.IndexOf)];
            var oldIndices = targetKeyframes.Select(KeyFrames.IndexOf).ToArray();

            foreach (var k in targetKeyframes)
            {
                KeyFrames.Remove(k);
            }

            HistoryModel.Add(new DeleteKeyFramesHistoryCommand(this, targetKeyframes, oldIndices, isCut));
            ValueCommited?.Invoke(this, EventArgs.Empty);
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
                var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - nk.Time) < TimeCalc.TimeEpsilon || k.Time <= nk.Time) + 1;
                if (index > 0 && Math.Abs(KeyFrames[index - 1].Time - nk.Time) < TimeCalc.TimeEpsilon)
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
            HistoryModel.Add(new ReplaceKeyFramesHistoryCommand(this, [..oldKeyFrames], newKeyFrames, historyNameKey));
            ValueCommited?.Invoke(this, EventArgs.Empty);
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
}
