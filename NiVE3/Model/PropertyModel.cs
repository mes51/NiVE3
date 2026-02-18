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
using NiVE3.Expression;
using System.Diagnostics.CodeAnalysis;
using Jint;
using Jint.Runtime;
using Acornima;
using NiVE3.Plugin.ValueObject;
using NiVE3.Cache;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.Model
{
    [UseReactiveProperty]
    partial class PropertyModel : BindableBase, IPropertyModel
    {
        public const string RawValueUpdateKey = nameof(RawValue);

        public string Name { get; }

        public bool IsEnable => true;

        [ReactiveProperty]
        public partial Time SourceStartPoint { get; set; }

        [ReactiveProperty]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<KeyFrame> KeyFrames { get; set; } = [];

        [ReactiveProperty]
        public partial bool UseExpression { get; set; }

        [ReactiveProperty]
        public partial bool UseEditingValue { get; set; }

        public string ExpressionCode
        {
            get;
            set
            {
                var oldIsEmpty = string.IsNullOrEmpty(field);
                if (!oldIsEmpty && string.IsNullOrEmpty(value))
                {
                    HistoryModel.HistoryChanged -= HistoryModel_HistoryChanged;
                }
                SetProperty(ref field, value);
                if (oldIsEmpty && !string.IsNullOrEmpty(value))
                {
                    HistoryModel.HistoryChanged += HistoryModel_HistoryChanged;
                }
            }
        } = "";

        [ReactiveProperty]
        public partial string ExpressionErrorMessage { get; set; } = "";

        [ReactiveProperty]
        public partial SourceLocation ExpressionErrorSourceLocation { get; set; }

        [ReactiveProperty]
        public partial bool ParentLayerIsLock { get; set; }

        public ObservableCollection<IPropertyModel>? Children => null;

        public PropertyBase Property { get; }

        public IPropertyModel ParentPropertyModel { get; }

        public Int128 ObjectId { get; }

        public string Id => Property.Id;

        public bool HasExpressionError => !string.IsNullOrEmpty(ExpressionErrorMessage);

        [MemberNotNullWhen(true, nameof(CompiledScript))]
        public bool IsEnableExpression => UseExpression && !HasExpressionError && CompiledScript != null;

        public Guid ParentLayerId => LayerModel.LayerId;

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        public event EventHandler<EventArgs>? ExpressionUpdated;

        public event EventHandler<EventArgs>? ValueInvalidateByHistoryChanged;

        [ReactiveProperty]
        private partial object? RawValue { get; set; }

        ProjectModel ProjectModel { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        EffectModel? EffectModel { get; }

        MaskModel? MaskModel { get; }

        HistoryModel HistoryModel { get; }

        ExpressionScript? CompiledScript { get; set; }

        CompositionViewModelProxy CompositionProxy { get; }

        LayerViewModelProxy LayerProxy { get; }

        EffectViewModelProxy? EffectProxy { get; }

        public PropertyModel(PropertyBase property, IPropertyModel parentPropertyModel, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel) : this(property, parentPropertyModel, projectModel, compositionModel, layerModel, null, null, historyModel) { }

        public PropertyModel(PropertyBase property, IPropertyModel parentPropertyModel, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, MaskModel? maskModel, HistoryModel historyModel)
        {
            Property = property;
            ParentPropertyModel = parentPropertyModel;
            ProjectModel = projectModel;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            MaskModel = maskModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            RawValue = property.DefaultValue;
            SourceStartPoint = layerModel.SourceStartPoint;
            ParentLayerIsLock = layerModel.IsLock;
            CurrentTime = compositionModel.CurrentTime;
            CompositionProxy = new CompositionViewModelProxy(compositionModel);
            LayerProxy = new LayerViewModelProxy(layerModel);
            EffectProxy = effectModel != null ? new EffectViewModelProxy(effectModel) : null;

            var objectIdHash = new XxHash3();
            objectIdHash.Append(parentPropertyModel.ObjectId);
            objectIdHash.Append(property.Id);
            ObjectId = objectIdHash.ToInt128();

            // NOTE: 本来はモデル側から設定してもらうものだが、引き回しの経路が複雑になりすぎる(レイヤーからだったり、エフェクトやマスクだったり)ため、自分から取りに行く
            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;
            layerModel.PropertyChanged += LayerModel_PropertyChanged;

            PropertyChanged += PropertyModel_PropertyChanged;
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            return Property.CreateControl(CompositionProxy, LayerProxy, EffectProxy, viewModel);
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(CompositionProxy, LayerProxy, EffectProxy, viewModel);
        }

        public bool ClearExpressionError()
        {
            ExpressionErrorMessage = "";
            ExpressionErrorSourceLocation = new SourceLocation();
            return UseExpression && CompiledScript != null;
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
                    RawValue = newValue;
                    HistoryModel.Add(new ValueChangeHistoryCommand(this, prevValue, newValue));
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void ReplaceKeyFrameValue(object? newValue, Time time)
        {
            for (var i = 0; i < KeyFrames.Count; i++)
            {
                if (KeyFrames[i].Time == time)
                {
                    var keyFrame = KeyFrames[i];
                    var newKeyFrame = new KeyFrame(keyFrame.Time, newValue, keyFrame.EaseIn, keyFrame.EaseOut, keyFrame.InterpolationType);

                    ReplaceKeyFrames([keyFrame], [newKeyFrame], LanguageResourceDictionary.History_ReplaceKeyFrames);
                    break;
                }
            }
        }

        public void CreateKeyFrame(object? value)
        {
            var time = CurrentTime - SourceStartPoint;
            var prevKeyFrame = KeyFrames.LastOrDefault(k => k.Time <= time);
            if (prevKeyFrame?.Time == time && prevKeyFrame.Value == value)
            {
                return;
            }

            var interpolationType = InterpolationType.None;
            if (prevKeyFrame != null)
            {
                interpolationType = prevKeyFrame.InterpolationType;
            }
            else
            {
                var supportedInterpolationType = Property.PropertyType.SupportedInterpolationTypes;
                if (supportedInterpolationType.HasFlag(InterpolationType.Linear))
                {
                    interpolationType = InterpolationType.Linear;
                }
                else if (supportedInterpolationType.HasFlag(InterpolationType.CatmullRom))
                {
                    interpolationType = InterpolationType.CatmullRom;
                }
                // TODO: グラフエディタ実装後
                //else if (supportedInterpolationType.HasFlag(InterpolationType.Bezier))
                //{
                //    interpolationType = InterpolationType.Bezier;
                //}
            }
            var keyFrame = new KeyFrame(time, value, new Ease(0.0, 0.0), new Ease(0.0, 0.0), interpolationType);
            var index = KeyFrames.FindLastIndex(k => Time.Abs(k.Time - time) < TimeCalc.TimeEpsilon || k.Time <= time) + 1;
            if (index > 0 && Time.Abs(KeyFrames[index - 1].Time - time) < TimeCalc.TimeEpsilon)
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
            var oldRawValue = RawValue;
            var newRawValue = GetRawValue(CurrentTime);
            KeyFrames.Clear();
            RawValue = newRawValue;
            HistoryModel.Add(new ClearKeyFramesHistoryCommand(this, keyFrames, oldRawValue, newRawValue));
            ValueCommited?.Invoke(this, EventArgs.Empty);
        }

        public void ReplaceAllKeyFrames(KeyFrame[] newKeyFrames)
        {
            ReplaceKeyFrames([..KeyFrames], newKeyFrames, LanguageResourceDictionary.History_ReplaceKeyFrames);
        }

        public void ChangeExpressionCode(string newExpressionCode)
        {
            if (ExpressionCode == newExpressionCode)
            {
                return;
            }

            var oldCode = ExpressionCode;
            var oldUseExpression = UseExpression;

            ExpressionCode = newExpressionCode;
            UseExpression = !string.IsNullOrEmpty(newExpressionCode);
            OnExpressionUpdated();

            HistoryModel.Add(new ChangeExpressionCodeHistoryCommand(this, oldCode, oldUseExpression, newExpressionCode, UseExpression));
        }

        public void ChangeUseExpression(bool useExpression)
        {
            if (UseExpression == useExpression)
            {
                return;
            }

            var oldUseExpression = UseExpression;
            UseExpression = useExpression;
            OnExpressionUpdated();

            HistoryModel.Add(new ChangeUseExpressionHistoryCommand(this, oldUseExpression, useExpression));
        }

        public void MoveTimeKeyFrames(KeyFrame[] targetKeyFrames, Time[] newTime)
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

        public void UpdateUncommitedRawValue(object? value)
        {
            RawValue = value;
        }

        public object? GetRawValue(Time layerTime)
        {
            if (UseEditingValue || KeyFrames.Count < 1)
            {
                return RawValue;
            }
            else
            {
                return Property.PropertyType.Interpolate(KeyFrames, layerTime);
            }
        }

        object? IPropertyObject.GetValue(Time layerTime, bool withoutDisableProperty)
        {
            return GetValue(layerTime, layerTime + SourceStartPoint);
        }

        public object? GetValue(Time time, Time globalTime)
        {
            if (PropertyValueCache.TryGet(ObjectId, time, out object? cacheValue))
            {
                return cacheValue;
            }

            var rawValue = GetRawValue(time);
            var value = rawValue;

            if (IsEnableExpression)
            {
                using var entry = CycleChecker.TryEnter(ObjectId);
                if (entry != null)
                {
                    var expressionValue = ToExpressionValue(rawValue);

                    try
                    {
                        using var context = ExpressionEngine.CreateContext(globalTime, ProjectModel, CompositionModel, LayerModel, EffectModel, MaskModel, this);
                        var expressionResult = context.Evaluate(CompiledScript, expressionValue);

                        if (Property.PropertyType.TryConvertFromExpressionValue(expressionResult, rawValue, out var newValue))
                        {
                            value = Property switch
                            {
                                CompositionDependPropertyBase cp => cp.CoerceValue(newValue, CompositionModel),
                                LayerDependPropertyBase lp => lp.CoerceValue(newValue, LayerModel),
                                _ => Property.CoerceValue(newValue)
                            };
                        }
                        else
                        {
                            ExpressionErrorMessage = "Expression result is invalid";
                            ExpressionErrorSourceLocation = GetLastLocation();
                        }
                    }
                    catch (JavaScriptException ex)
                    {
                        ExpressionErrorMessage = ex.Message;
                        ExpressionErrorSourceLocation = ex.Location;
                    }
                    catch (TimeoutException ex)
                    {
                        ExpressionErrorMessage = ex.Message;
                        ExpressionErrorSourceLocation = GetLastLocation();
                    }
                }
            }

            PropertyValueCache.Upsert(ObjectId, time, value);
            return value;
        }

        public object? ToExpressionValue(object? value)
        {
            if (Property.PropertyType is ICompositionDependIPropertyType cpt)
            {
                return cpt.ConvertToExpressionValue(value, CompositionModel);
            }
            else
            {
                return Property.PropertyType.ConvertToExpressionValue(value);
            }
        }

        public object? GetCurrentTimeValue()
        {
            var time = CurrentTime - SourceStartPoint;
            return GetValue(time, CurrentTime);
        }

        public void ResetProperty()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ResetPropertyValue));

            if (HasKeyFrame())
            {
                CreateKeyFrame(Property.DefaultValue);
            }
            else
            {
                CommitProperty(Property.DefaultValue, RawValue);
            }

            HistoryModel.EndGroup();
        }

        public void ClearProperty()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ResetPropertyValue));

            ClearKeyFrame();
            CommitProperty(Property.DefaultValue, RawValue);

            HistoryModel.EndGroup();
        }

        PropertyValueGroup? IPropertyObject.GetValues(Time layerTime, bool withoutDisableProperty)
        {
            return null;
        }

        public void CalcValuesHash(XxHash3 hash)
        {
            hash.Append(ObjectId);
            if (KeyFrames.Count > 0)
            {
                foreach (var keyFrame in KeyFrames)
                {
                    hash.Append((double)keyFrame.Time);
                    hash.Append(Property.PropertyType.ConvertToHashBase(keyFrame.Value));
                    hash.Append(keyFrame.InterpolationType);
                }
            }
            else
            {
                hash.Append(Property.PropertyType.ConvertToHashBase(RawValue));
            }

            hash.Append(UseExpression);
            if (UseExpression)
            {
                hash.Append(ExpressionCode);
            }
        }

        public void UpdateValueByCompositionStateChanged()
        {
            if (Property is CompositionDependPropertyBase cp)
            {
                var oldValue = RawValue;
                RawValue = cp.ChangeValueByCompositionStateChanged(RawValue, CompositionModel);

                if (oldValue != RawValue)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByCompositionStateChangedHistoryCommand(this, oldValue, RawValue));
                }
            }
        }

        public void UpdateValueByLayerStateChanged()
        {
            if (Property is LayerDependPropertyBase lp)
            {
                var oldValue = RawValue;
                RawValue = lp.ChangeValueByLayerStateChanged(RawValue, LayerModel);

                if (oldValue != RawValue)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByLayerStateChangedHistoryCommand(this, oldValue, RawValue));
                }
            }
        }

        public void UpdateValueByReplacedEffectId(Dictionary<Guid, Guid> effectIdMap)
        {
            if (Property is LayerDependPropertyBase lp)
            {
                var oldValue = RawValue;
                RawValue = lp.ChangeValueByReplaceEffectId(RawValue, effectIdMap, LayerModel);

                if (oldValue != RawValue)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByReplacedObjectIdHistoryCommand(this, oldValue, RawValue));
                }
            }
        }

        public void UpdateValueByReplacedMaskId(Dictionary<Guid, Guid> maskIdMap)
        {
            if (Property is LayerDependPropertyBase lp)
            {
                var oldValue = RawValue;
                RawValue = lp.ChangeValueByReplaceMaskId(RawValue, maskIdMap, LayerModel);

                if (oldValue != RawValue)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByReplacedObjectIdHistoryCommand(this, oldValue, RawValue));
                }
            }
        }

        public void UpdateValueByReplacedLayerId(Dictionary<Guid, Guid> layerIdMap)
        {
            if (Property is CompositionDependPropertyBase cp)
            {
                var oldValue = RawValue;
                RawValue = cp.ChangeValueByReplaceLayerId(RawValue, layerIdMap, CompositionModel);

                if (oldValue != RawValue)
                {
                    ValueCommited?.Invoke(this, EventArgs.Empty);
                    HistoryModel.Add(new UpdateValueByReplacedObjectIdHistoryCommand(this, oldValue, RawValue));
                }
            }
        }

        public bool HasCompositionDependProperty()
        {
            return Property is CompositionDependPropertyBase;
        }

        public bool HasKeyFrame()
        {
            return KeyFrames.Count > 0;
        }

        public bool IsChangeableByTime()
        {
            return IsEnableExpression;
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
                Value = Property.PropertyType.SerializeValue(RawValue),
                ExpressionCode = ExpressionCode,
                UseExpression = UseExpression,
                KeyFrames = keyFramesData
            };
        }

        public void LoadData(PropertyData data)
        {
            RawValue = Property.PropertyType.DeserializeValue(data.Value);
            if (data.KeyFrames == null)
            {
                return;
            }

            KeyFrames.Clear();
            foreach (var k in data.KeyFrames.Select(k => new KeyFrame(k.Time, Property.PropertyType.DeserializeValue(k.Value), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
            {
                KeyFrames.Add(k);
            }

            ExpressionCode = data.ExpressionCode;
            UseExpression = data.UseExpression;
        }

        public void CoerceValues()
        {
            var oldKeyFrames = KeyFrames.ToArray();
            KeyFrames.Clear();

            if (Property is CompositionDependPropertyBase cp)
            {
                RawValue = cp.CoerceValue(RawValue, CompositionModel);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, cp.CoerceValue(k.Value, CompositionModel), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
            else if (Property is LayerDependPropertyBase lp)
            {
                RawValue = lp.CoerceValue(RawValue, LayerModel);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, lp.CoerceValue(k.Value, LayerModel), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
            else
            {
                RawValue = Property.CoerceValue(RawValue);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, Property.CoerceValue(k.Value), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
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
                    LayerDependPropertyBase lp => lp.CoerceValue(Property.PropertyType.DeserializeValue(data.Value), LayerModel),
                    _ => Property.CoerceValue(Property.PropertyType.DeserializeValue(data.Value))
                };
                var oldValue = RawValue;
                var oldExpressionCode = ExpressionCode;
                var oldUseExpression = UseExpression;

                RawValue = newValue;
                ExpressionCode = data.ExpressionCode;
                UseExpression = data.UseExpression;

                OnExpressionUpdated();

                HistoryModel.Add(new PastePropertyHistoryCommand(this, oldValue, oldExpressionCode, oldUseExpression, newValue, ExpressionCode, UseExpression));
            }
            else
            {
                var startTime = keyFrames[0].Time;
                var newKeyFrames = new List<KeyFrame>();
                if (Property is not CompositionDependPropertyBase && Property is not LayerDependPropertyBase)
                {
                    foreach (var k in keyFrames)
                    {
                        var newTime = k.Time - startTime + CurrentTime - SourceStartPoint;
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
                    var index = KeyFrames.FindLastIndex(k => Time.Abs(k.Time - nk.Time) < TimeCalc.TimeEpsilon || k.Time <= nk.Time) + 1;
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
            var oldValue = RawValue;

            LoadData(data);

            HistoryModel.Add(new OverwritePropertyHistoryCommand(this, oldKeyFrames, oldValue, [..KeyFrames], RawValue));
        }

        public bool IsAlive(IPropertyModel child)
        {
            return ParentPropertyModel.IsAlive(this);
        }

        public CopyData<PropertyData> CopyProperty()
        {
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Value = Property.PropertyType.SerializeValue(RawValue),
                ExpressionCode = ExpressionCode,
                UseExpression = UseExpression
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

        public void PasteExpressionOnly(PropertyData data)
        {
            var oldCode = ExpressionCode;
            var oldUseExpression = UseExpression;

            ExpressionCode = data.ExpressionCode;
            UseExpression = data.UseExpression;
            OnExpressionUpdated();

            HistoryModel.Add(new ChangeExpressionCodeHistoryCommand(this, oldCode, oldUseExpression, ExpressionCode, UseExpression));
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
                Value = Property.PropertyType.SerializeValue(RawValue),
                KeyFrames = keyFramesData
            };

            return new CopyData<PropertyData>(CopyDataType.KeyFrame, [data]);
        }

        public (IDisposable compositionEntry, IDisposable layerEntry, IDisposable? effectEntry)? EnterCycricForCalcProperty()
        {
            var compositionEntry = CycleChecker.TryEnter(CompositionModel.CompositionId);
            var layerEntry = CycleChecker.TryEnter(LayerModel.LayerId);

            if (compositionEntry == null || layerEntry == null)
            {
                layerEntry?.Dispose();
                compositionEntry?.Dispose();
                return null;
            }
            else
            {
                return (compositionEntry, layerEntry, EffectModel != null ? CycleChecker.TryEnter(EffectModel.EffectId) : null);
            }
        }

        void DeleteKeyFramesInternal(KeyFrame[] targetKeyframes, bool isCut)
        {
            targetKeyframes = [.. targetKeyframes.OrderBy(KeyFrames.IndexOf)];
            var oldIndices = targetKeyframes.Select(KeyFrames.IndexOf).ToArray();

            var oldRawValue = RawValue;
            var newRawValue = GetRawValue(CurrentTime);

            foreach (var k in targetKeyframes)
            {
                KeyFrames.Remove(k);
            }

            if (KeyFrames.Count < 1)
            {
                RawValue = newRawValue;
            }

            HistoryModel.Add(new DeleteKeyFramesHistoryCommand(this, targetKeyframes, oldIndices, oldRawValue, newRawValue, isCut));
            ValueCommited?.Invoke(this, EventArgs.Empty);
        }

        void ReplaceKeyFrames(KeyFrame[] targetKeyFrames, KeyFrame[] newKeyFrames, string historyNameKey)
        {
            var hasExpressionError = HasExpressionError;

            foreach (var k in targetKeyFrames)
            {
                KeyFrames.Remove(k);
            }

            var oldKeyFrames = new List<KeyFrame>();
            oldKeyFrames.AddRange(targetKeyFrames);
            foreach (var nk in newKeyFrames)
            {
                var index = KeyFrames.FindLastIndex(k => Time.Abs(k.Time - nk.Time) < TimeCalc.TimeEpsilon || k.Time <= nk.Time) + 1;
                if (index > 0 && Time.Abs(KeyFrames[index - 1].Time - nk.Time) < TimeCalc.TimeEpsilon)
                {
                    oldKeyFrames.Add(KeyFrames[index - 1]);
                    KeyFrames[index - 1] = nk;
                }
                else
                {
                    KeyFrames.Insert(index, nk);
                }
            }

            if (UseExpression && !hasExpressionError)
            {
                ClearExpressionError();
            }

            oldKeyFrames.Sort((a, b) => a.Time.CompareTo(b.Time));
            HistoryModel.Add(new ReplaceKeyFramesHistoryCommand(this, [..oldKeyFrames], newKeyFrames, historyNameKey));
            ValueCommited?.Invoke(this, EventArgs.Empty);
        }

        SourceLocation GetLastLocation()
        {
            var lines = ExpressionCode.Split("\n").Select(l => l.Replace("\r", "")).Reverse().SkipWhile(string.IsNullOrEmpty).ToArray();
            var lastLine = lines.FirstOrDefault("");
            return SourceLocation.From(Position.From(lines.Length, lastLine.Length - 1), Position.From(lines.Length, lastLine.Length));
        }

        void OnExpressionUpdated()
        {
            ExpressionUpdated?.Invoke(this, EventArgs.Empty);
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
            switch (e.PropertyName)
            {
                case nameof(LayerModel.SourceStartPoint):
                    SourceStartPoint = LayerModel.SourceStartPoint;
                    break;
                case nameof(LayerModel.IsLock):
                    ParentLayerIsLock = LayerModel.IsLock;
                    break;
            }
        }

        private void PropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RawValue):
                    ValueUpdated?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(ExpressionCode):
                    CompiledScript?.Dispose();
                    if (string.IsNullOrEmpty(ExpressionCode))
                    {
                        CompiledScript = null;
                        ExpressionErrorMessage = "";
                        ExpressionErrorSourceLocation = new SourceLocation();
                    }
                    else
                    {
                        try
                        {
                            CompiledScript = ExpressionEngine.Compile(ExpressionCode);
                            ExpressionErrorMessage = "";
                            ExpressionErrorSourceLocation = new SourceLocation();
                        }
                        catch (ScriptPreparationException ex)
                        {
                            CompiledScript = null;
                            if (ex.InnerException is ParseErrorException pex)
                            {
                                ExpressionErrorMessage = pex.Message;
                                ExpressionErrorSourceLocation = SourceLocation.From(pex.Error.Position, pex.Error.Position);
                            }
                            else
                            {
                                ExpressionErrorMessage = ex.Message;
                            }
                        }
                    }
                    break;
            }
        }

        private void HistoryModel_HistoryChanged(object? sender, EventArgs e)
        {
            ValueCommited?.Invoke(this, EventArgs.Empty);
            ValueInvalidateByHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        ~PropertyModel()
        {
            CompiledScript?.Dispose();
        }
    }
}
