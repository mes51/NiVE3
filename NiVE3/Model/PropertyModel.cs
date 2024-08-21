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
using NiVE3.Data.Json.Project;
using NiVE3.Extension;
using NiVE3.Util;
using NiVE3.Data.Clipboard;
using NiVE3.Plugin.Property.Types;
using ImTools;
using System.Collections.Specialized;
using NiVE3.Mvvm;
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ViewModelWireGenerator;

namespace NiVE3.Model
{
    interface IPropertyModel : IPropertyObject
    {
        string Name { get; }

        object? Value { get; }

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollection<IPropertyModel>? Children { get; }

        PropertyBase Property { get; }

        event EventHandler<EventArgs>? ValueUpdated;

        event EventHandler<EventArgs>? ValueCommited;

        PropertyControlBase CreateControl(IPropertyViewModel viewModel);

        PropertyViewState CreateState(IPropertyViewModel propertyViewModel);

        void UpdateValueByCompositionStateChanged();

        bool HasCompositionDependProperty();

        bool HasKeyFrames();

        PropertyData SaveData();

        void LoadData(PropertyData data);

        void PasteProperty(PropertyData data);
    }

    file interface IOverwriteablePropertyModel : IPropertyModel
    {
        void OverwriteProperty(PropertyData data);
    }

    partial class PropertyModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        public string Name { get; }

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

        public string Id => Property.Id;

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

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
            Name = property.DisplayName;
            Value = property.DefaultValue;
            SourceStartPoint = layerModel?.SourceStartPoint ?? 0.0;
            CurrentTime = compositionModel.CurrentTime;

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

        public PropertyValueGroup? GetValues(double time)
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
            var cp = Property as CompositionDependPropertyBase;

            if (cp != null)
            {
                Value = cp.CoerceValue(cp.PropertyType.DeserializeValue(data.Value), CompositionModel);
            }
            else
            {
                Value = Property.CoerceValue(Property.PropertyType.DeserializeValue(data.Value));
            }

            if (data.KeyFrames == null)
            {
                return;
            }

            KeyFrames.Clear();
            if (cp != null)
            {
                foreach (var k in data.KeyFrames.Select(k => new KeyFrame(k.Time, cp.CoerceValue(cp.PropertyType.DeserializeValue(k.Value), CompositionModel), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
            else
            {
                foreach (var k in data.KeyFrames.Select(k => new KeyFrame(k.Time, Property.CoerceValue(Property.PropertyType.DeserializeValue(k.Value)), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
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

    partial class PropertyGroupModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public Guid InstanceId { get; }

        public object? Value => null;

        public ObservableCollection<KeyFrame>? KeyFrames => null;

        private ObservableCollection<IPropertyModel> children = [];
        public ObservableCollection<IPropertyModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public PropertyBase Property { get; }

        public string Id => Property.Id;

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, HistoryModel historyModel, Guid? instanceId = null) : this(property, compositionModel, null, null, historyModel, instanceId) { }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, HistoryModel historyModel, Guid? instanceId = null) : this(property, compositionModel, layerModel, null, historyModel, instanceId) { }

        public PropertyGroupModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, HistoryModel historyModel, Guid? instanceId = null)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            InstanceId = instanceId ?? Guid.NewGuid();

            foreach (var c in ((PropertyGroup)property).Children)
            {
                if (c is PropertyGroup)
                {
                    Children.Add(new PropertyGroupModel(c, compositionModel, layerModel, effectModel, historyModel));
                }
                else if (c is AppendableProperty)
                {
                    Children.Add(new AppendablePropertyModel(c, compositionModel, layerModel, effectModel, historyModel));
                }
                else
                {
                    Children.Add(new PropertyModel(c, compositionModel, layerModel, effectModel, historyModel));
                }
            }

            foreach (var c in Children)
            {
                c.ValueUpdated += Child_ValueUpdated;
                c.ValueCommited += Child_ValueCommited;
            }
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
        }

        public IReadOnlyCollection<IPropertyObject>? GetChildren()
        {
            return Children;
        }

        public object? GetValue(double time)
        {
            return null;
        }

        public PropertyValueGroup GetValues(double time)
        {
            var result = new Dictionary<string, object?>();
            var propertyTypes = new Dictionary<string, IPropertyType>();

            foreach (var p in Children)
            {
                if (p is PropertyGroupModel pg)
                {
                    result.Add(pg.Property.Id, pg.GetValues(time));
                }
                else if (p is AppendablePropertyModel ap)
                {
                    result.Add(ap.Property.Id, ap.GetChildPropertyValues(time));
                }
                else if (p is PropertyModel pp)
                {
                    result.Add(pp.Property.Id, pp.GetValue(time));
                }

                propertyTypes.Add(p.Property.Id, p.Property.PropertyType);
            }

            return new PropertyValueGroup(Property.Id, result, propertyTypes);
        }

        public void UpdateValueByCompositionStateChanged()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByCompositionStateChanged));

            foreach (var child in Children)
            {
                child.UpdateValueByCompositionStateChanged();
            }

            HistoryModel.EndGroup();
        }

        public bool HasCompositionDependProperty()
        {
            return Children.Any(c => c.HasCompositionDependProperty());
        }

        public bool HasKeyFrames()
        {
            return Children.Any(p => p.HasKeyFrames());
        }

        public void ChangeName(string name)
        {
            if (name != Name)
            {
                var oldNeme = Name;
                Name = name;

                HistoryModel.Add(new ChangeNameHistoryCommand(this, oldNeme, name));
            }
        }

        public IPropertyModel? FindProperty(string propertyId)
        {
            var child = Children.FirstOrDefault(c => c.Property.Id == propertyId);
            if (child != null)
            {
                return child;
            }

            foreach (var childGroup in Children.OfType<PropertyGroupModel>())
            {
                var grandChild = childGroup.FindProperty(propertyId);
                if (grandChild != null)
                {
                    return grandChild;
                }
            }

            return null;
        }

        public PropertyData SaveData()
        {
            return new PropertyData
            {
                PropertyId = Property.Id,
                InstanceId = InstanceId,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = Children.Select(p => p.SaveData()).ToArray()
            };
        }

        public void LoadData(PropertyData data)
        {
            Name = data.Name;
            if (data.Children == null)
            {
                return;
            }

            foreach (var childData in data.Children)
            {
                Children.FirstOrDefault(c => c.Property.Id == childData.PropertyId)?.LoadData(childData);
            }
        }

        public void PasteProperty(PropertyData data)
        {
            PasteChildrenPropertyInternal(data, []);
        }

        public void OverwriteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName || data.Children == null)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteProperty));

            var targetChildren = Children.Where(c => data.Children.Any(d => d.PropertyId == c.Property.Id)).OrderBy(c => data.Children.IndexOf(d => d.PropertyId == c.Property.Id)).OfType<IOverwriteablePropertyModel>();
            foreach (var (childData, child) in data.Children.Zip(targetChildren))
            {
                child.OverwriteProperty(childData);
            }

            HistoryModel.EndGroup();
        }

        public CopyData<PropertyData> CopyChildrenProperty(string[] ids)
        {
            var children = Children.Where(p => ids.Contains(p.Property.Id)).OrderBy(Children.IndexOf);
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                InstanceId = InstanceId,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = [.. children.Select(c => c.SaveData())]
            };

            return new CopyData<PropertyData>(CopyDataType.PropertyGroup, [data]);
        }

        public void PasteChildrenProperty(CopyData<PropertyData> data, string[] ids)
        {
            if (data.Data.Length < 1)
            {
                return;
            }

            if (data.Type == CopyDataType.PropertyGroup)
            {
                PasteChildrenPropertyInternal(data.Data[0], ids);
            }
            else if (ids.Length == 1 && Children.FirstOrDefault(c => c.Property.Id == ids[0]) is IPropertyModel targetChild)
            {
                targetChild.PasteProperty(data.Data[0]);
            }
        }

        public CopyData<PropertyData> CutChildrenKeyFrames(string[] ids)
        {
            var data = CopyChildrenProperty(ids);
            DeleteChildrenKeyFramesInternal(ids, true);

            return data;
        }

        public void DeleteChildrenKeyFrames(string[] ids)
        {
            DeleteChildrenKeyFramesInternal(ids, false);
        }

        void DeleteChildrenKeyFramesInternal(string[] ids, bool isCut)
        {
            var children = Children.OfType<PropertyModel>().Where(p => ids.Contains(p.Property.Id));
            if (children.All(c => c.KeyFrames.Count < 1))
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveKeyFrame));
            foreach (var child in children)
            {
                child.ClearKeyFrame();
            }
            HistoryModel.EndGroup();
        }

        void PasteChildrenPropertyInternal(PropertyData data, string[] ids)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName || data.Children == null || data.Children.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteProperty));
            if (data.Children.Length > 1)
            {
                // NOTE: 複数同時貼り付けの場合は同一のグループにしか貼り付けられない
                if (data.PropertyId == Property.Id)
                {
                    var targetChildren = Children.Where(c => data.Children.Any(d => d.PropertyId == c.Property.Id)).OrderBy(c => data.Children.IndexOf(d => d.PropertyId == c.Property.Id)).OfType<IOverwriteablePropertyModel>();
                    if (ids.Length > 0)
                    {
                        targetChildren = targetChildren.Where(c => ids.Contains(c.Property.Id));
                    }
                    foreach (var (childData, child) in data.Children.Zip(targetChildren))
                    {
                        switch (child)
                        {
                            case PropertyModel:
                            case AppendableProperty:
                                child.PasteProperty(childData);
                                break;
                            default:
                                // NOTE: グループの貼り付けは孫以降すべて上書きする
                                child.OverwriteProperty(childData);
                                break;
                        }
                        child.OverwriteProperty(childData);
                    }
                    HistoryModel.EndGroup();
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }
            else if (ids.Length > 0)
            {
                var propertyData = data.Children[0];
                var target = Children.Where(c => c.Property.PropertyType.GetType().FullName == propertyData.PropertyTypeName && ids.Contains(c.Property.Id));

                var pasted = false;
                foreach (var t in target)
                {
                    t.PasteProperty(propertyData);
                    pasted = true;
                }

                if (pasted)
                {
                    HistoryModel.EndGroup();
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }
            else
            {
                HistoryModel.AbortGroup();
            }
        }

        private void Child_ValueUpdated(object? sender, EventArgs e)
        {
            ValueUpdated?.Invoke(sender, e);
        }

        private void Child_ValueCommited(object? sender, EventArgs e)
        {
            ValueCommited?.Invoke(sender, e);
        }
    }

    partial class AppendablePropertyModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        public string Name { get; }

        public object? Value => null;

        public ObservableCollection<KeyFrame>? KeyFrames => null;

        private ObservableCollection<IPropertyModel> children = [];
        public ObservableCollection<IPropertyModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public PropertyBase Property { get; }

        public string Id => Property.Id;

        public AppendablePropertyItem[] Items { get; }

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public AppendablePropertyModel(PropertyBase property, CompositionModel compositionModel, HistoryModel historyModel) : this(property, compositionModel, null, null, historyModel) { }

        public AppendablePropertyModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, HistoryModel historyModel) : this(property, compositionModel, layerModel, null, historyModel) { }

        public AppendablePropertyModel(PropertyBase property, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;

            var ap = (AppendableProperty)property;
            Items = ap.Items;
            if (ap.DefaultAppendedItem != null)
            {
                AddChildInternal(ap.DefaultAppendedItem, null);
            }
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
        }

        public IReadOnlyCollection<IPropertyObject>? GetChildren()
        {
            return Children;
        }

        public object? GetValue(double time)
        {
            return GetChildPropertyValues(time);
        }

        public PropertyValueGroup? GetValues(double time)
        {
            return null;
        }

        public void UpdateValueByCompositionStateChanged()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByCompositionStateChanged));

            foreach (var child in Children)
            {
                child.UpdateValueByCompositionStateChanged();
            }

            HistoryModel.EndGroup();
        }

        public bool HasCompositionDependProperty()
        {
            return Children.Any(c => c.HasCompositionDependProperty());
        }

        public bool HasKeyFrames()
        {
            return Children.Any(p => p.HasKeyFrames());
        }

        public void AddChild(AppendablePropertyItem item)
        {
            var child = AddChildInternal(item, null);
            ValueCommited?.Invoke(this, EventArgs.Empty);

            HistoryModel.Add(new AddAppendablePropertyChildHistoryCommand(this, child, Children.IndexOf(child)));
        }

        public void DeleteChildren(Guid[] propertyInstanceIds)
        {
            DeleteChildrenInternal(propertyInstanceIds, false);
        }

        public void MoveChild(Guid propertyInstanceId, int newIndex)
        {
            MoveChildren([propertyInstanceId], propertyInstanceId, newIndex);
        }

        public void MoveChildren(Guid[] propertyInstanceIds, Guid referencePropertyInstanceId, int newIndex)
        {
            if (Children.Count == propertyInstanceIds.Length)
            {
                return;
            }

            var prevOrderedChildren = Children.OfType<PropertyGroupModel>().ToArray();
            var targetGroups = prevOrderedChildren.Where(g => propertyInstanceIds.Contains(g.InstanceId)).ToArray();
            var prevIndices = targetGroups.Select(Children.IndexOf).ToArray();
            var startIndex = newIndex - targetGroups.IndexOf(c => c.InstanceId == referencePropertyInstanceId);
            var newOrderedChildren = new List<IPropertyModel>(prevOrderedChildren.Length);
            newOrderedChildren.AddRange(prevOrderedChildren.Except(targetGroups).Take(startIndex));
            newOrderedChildren.AddRange(targetGroups);
            newOrderedChildren.AddRange(prevOrderedChildren.Except(newOrderedChildren.ToArray()));

            Children.SortBy(newOrderedChildren.IndexOf);
            ValueCommited?.Invoke(this, EventArgs.Empty);

            if (!prevOrderedChildren.SequenceEqual(Children))
            {
                HistoryModel.Add(new MoveAppendablePropertyChildrenHistoryCommand(this, prevOrderedChildren, [..Children]));
            }
        }

        public PropertyValueGroup[] GetChildPropertyValues(double time)
        {
            return Children.OfType<PropertyGroupModel>().Select(m => m.GetValues(time)).ToArray();
        }

        public PropertyData SaveData()
        {
            return new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = Children.Select(p => p.SaveData()).ToArray()
            };
        }

        public void LoadData(PropertyData data)
        {
            if (data.Children == null)
            {
                return;
            }

            Children.Clear();

            foreach (var childData in data.Children)
            {
                var item = Items.FirstOrDefault(i => i.Id == childData.PropertyId);
                if (item == null)
                {
                    continue;
                }

                AddChildInternal(item, childData.InstanceId).LoadData(childData);
            }
        }

        public void PasteProperty(PropertyData data)
        {
            PasteChildrenInternal(data, false);
        }

        public void OverwriteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var oldChildren = Children.ToArray();

            LoadData(data);

            HistoryModel.Add(new OverwritePropertyHistoryCommand(this, oldChildren, [..Children]));
        }

        public CopyData<PropertyData> CutChildren(Guid[] propertyInstanceIds)
        {
            var result = CopyChildrenProperty(propertyInstanceIds);
            DeleteChildrenInternal(propertyInstanceIds, true);

            return result;
        }

        public CopyData<PropertyData> CopyChildrenProperty(Guid[] ids)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(p => ids.Contains(p.InstanceId)).OrderBy(Children.IndexOf);
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = [.. children.Select(c => c.SaveData())]
            };

            return new CopyData<PropertyData>(CopyDataType.AppendablePropertyChildren, [data]);
        }

        public void PasteChildrenProperty(CopyData<PropertyData> data, Guid? targetId)
        {
            if (data.Data.Length < 1)
            {
                return;
            }

            if (data.Type == CopyDataType.AppendablePropertyChildren && data.Data[0].PropertyId == Property.Id)
            {
                PasteChildrenInternal(data.Data[0], false);
            }
            else if (data.Type == CopyDataType.PropertyGroup &&
                Children.OfType<PropertyGroupModel>().FirstOrDefault(c => c.InstanceId == targetId) is IOverwriteablePropertyModel targetChild &&
                targetChild.Property.Id == data.Data[0].PropertyId)
            {
                targetChild.OverwriteProperty(data.Data[0]);
            }
        }

        public void DuplicateChildrenProperty(Guid[] ids)
        {
            var data = CopyChildrenProperty(ids);
            PasteChildrenInternal(data.Data[0], true);
        }

        PropertyGroupModel AddChildInternal(AppendablePropertyItem item, Guid? instanceId)
        {
            var group = item.CreateFunc();
            var groupModel = new PropertyGroupModel(group, CompositionModel, LayerModel, EffectModel, HistoryModel, instanceId);
            groupModel.ValueUpdated += Child_ValueUpdated;
            groupModel.ValueCommited += Child_ValueCommited;

            var newChildNumber = 1;
            var newName = "";
            while (true)
            {
                newName = $"{groupModel.Name} {newChildNumber}";
                if (Children.All(c => c.Name != newName))
                {
                    break;
                }
                newChildNumber++;
            }
            groupModel.Name = newName;

            Children.Add(groupModel);

            return groupModel;
        }

        void InsertInternal(int index, PropertyGroupModel child)
        {
            if (Items.All(i => i.Id != child.Property.Id))
            {
                return;
            }

            child.ValueUpdated += Child_ValueUpdated;
            child.ValueCommited += Child_ValueCommited;
            Children.Insert(index, child);
        }

        void RemoveInternal(PropertyGroupModel child)
        {
            if (Children.Remove(child))
            {
                child.ValueUpdated -= Child_ValueUpdated;
                child.ValueCommited -= Child_ValueCommited;
            }
        }

        void DeleteChildrenInternal(Guid[] propertyInstanceIds, bool isCut)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(c => propertyInstanceIds.Contains(c.InstanceId)).ToArray();
            var indices = children.Select(Children.IndexOf).ToArray();

            foreach (var c in children)
            {
                RemoveInternal(c);
            }
            ValueCommited?.Invoke(this, EventArgs.Empty);

            HistoryModel.Add(new DeleteAppendablePropertyChildHistoryCommand(this, children, indices, isCut));
        }

        void PasteChildrenInternal(PropertyData data, bool isDuplicate)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var newChildren = new List<PropertyGroupModel>();
            foreach (var childData in data.Children ?? [])
            {
                var item = Items.FirstOrDefault(i => i.Id == childData.PropertyId);
                if (item == null)
                {
                    continue;
                }

                var newChild = AddChildInternal(item, null);
                newChild.LoadData(childData);
                newChildren.Add(newChild);
            }

            HistoryModel.Add(new PastePropertyHistoryCommand(this, [.. newChildren], isDuplicate));
        }

        private void Child_ValueUpdated(object? sender, EventArgs e)
        {
            ValueUpdated?.Invoke(sender, e);
        }

        private void Child_ValueCommited(object? sender, EventArgs e)
        {
            ValueCommited?.Invoke(sender, e);
        }
    }

    class CompositionViewModelProxy : WeakPropertyChangedBindingBase, ICompositionViewModel
    {
        public IReadOnlyCollection<ILayerViewModel> LayerViewModels { get; }

        public CompositionViewModelProxy(CompositionModel composition)
        {
            LayerViewModels = composition.Layers.CreateViewCollection(l => new LayerViewModelProxy(l));
        }
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerViewModelProxy : WeakPropertyChangedBindingBase, ILayerViewModel
    {
        private Guid layerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid LayerId
        {
            get { return layerId; }
            set { SetProperty(ref layerId, value); }
        }

        private bool isEnable3D;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private string name = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string sourceName = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string SourceName
        {
            get { return sourceName; }
            set { SetProperty(ref sourceName, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        LayerModel LayerModel { get; }

        public LayerViewModelProxy(LayerModel layerModel)
        {
            LayerModel = layerModel;

            WiringModel();
        }

        partial void WiringModel();
    }

    class EffectViewModelProxy : WeakPropertyChangedBindingBase, IEffectViewModel
    {
#pragma warning disable IDE0052 // 読み取られていないプライベート メンバーを削除
        EffectModel EffectModel { get; }
#pragma warning restore IDE0052 // 読み取られていないプライベート メンバーを削除

        public EffectViewModelProxy(EffectModel effectModel)
        {
            EffectModel = effectModel;
        }
    }
}
