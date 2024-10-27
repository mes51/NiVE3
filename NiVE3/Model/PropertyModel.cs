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

namespace NiVE3.Model
{
    partial class PropertyModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        public const string RawValueUpdateKey = nameof(RawValue);

        public string Name { get; }

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

        private bool useExpression;
        public bool UseExpression
        {
            get { return useExpression; }
            set { SetProperty(ref useExpression, value); }
        }

        private bool useEditingValue;
        public bool UseEditingValue
        {
            get { return useEditingValue; }
            set { SetProperty(ref useEditingValue, value); }
        }

        private string expressionCode = "";
        public string ExpressionCode
        {
            get { return expressionCode; }
            set
            {
                var oldIsEmpty = string.IsNullOrEmpty(expressionCode);
                if (!oldIsEmpty && string.IsNullOrEmpty(value))
                {
                    HistoryModel.HistoryChanged -= HistoryModel_HistoryChanged;
                }
                SetProperty(ref expressionCode, value);
                if (oldIsEmpty && !string.IsNullOrEmpty(value))
                {
                    HistoryModel.HistoryChanged += HistoryModel_HistoryChanged;
                }
            }
        }

        private string expressionErrorMessage = "";
        public string ExpressionErrorMessage
        {
            get { return expressionErrorMessage; }
            set { SetProperty(ref expressionErrorMessage, value); }
        }

        private SourceLocation expressionErrorSourceLocation;
        public SourceLocation ExpressionErrorSourceLocation
        {
            get { return expressionErrorSourceLocation; }
            set { SetProperty(ref expressionErrorSourceLocation, value); }
        }

        public ObservableCollection<IPropertyModel>? Children => null;

        public PropertyBase Property { get; }

        public Int128 ObjectId { get; }

        public string Id => Property.Id;

        public bool HasExpressionError => !string.IsNullOrEmpty(ExpressionErrorMessage);

        [MemberNotNullWhen(true, nameof(CompiledScript))]
        public bool IsEnableExpression => UseExpression && !HasExpressionError && CompiledScript != null;

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        public event EventHandler<EventArgs>? ExpressionUpdated;

        public event EventHandler<EventArgs>? ValueInvalidateByHistoryChanged;

        private object? rawValue;
        object? RawValue
        {
            get { return rawValue; }
            set { SetProperty(ref rawValue, value); }
        }

        ProjectModel ProjectModel { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        ExpressionScript? CompiledScript { get; set; }

        public PropertyModel(PropertyBase property, Int128 parentObjectId, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel) : this(property, parentObjectId, projectModel, compositionModel, layerModel, null, historyModel) { }

        public PropertyModel(PropertyBase property, Int128 parentObjectId, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            ProjectModel = projectModel;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            RawValue = property.DefaultValue;
            SourceStartPoint = layerModel.SourceStartPoint;
            CurrentTime = compositionModel.CurrentTime;

            var objectIdHash = new XxHash3();
            objectIdHash.Append(parentObjectId);
            objectIdHash.Append(property.Id);
            ObjectId = objectIdHash.ToInt128();

            // NOTE: 本来はモデル側から設定してもらうものだが、引き回しの経路が複雑になりすぎる(レイヤーからだったり、エフェクトやマスクだったり)ため、自分から取りに行く
            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;
            layerModel.PropertyChanged += LayerModel_PropertyChanged;

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
                    RawValue = newValue;
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

        public void UpdateUncommitedRawValue(object? value)
        {
            RawValue = value;
        }

        public object? GetRawValue(double time)
        {
            if (UseEditingValue || KeyFrames.Count < 1)
            {
                return RawValue;
            }
            else
            {
                return Property.PropertyType.Interpolate(KeyFrames, time);
            }
        }

        object? IPropertyObject.GetValue(double layerTime)
        {
            return GetValue(layerTime, layerTime + SourceStartPoint);
        }

        public object? GetValue(double time, double globalTime)
        {
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
                        using var context = ExpressionEngine.CreateContext(globalTime, ProjectModel, CompositionModel, LayerModel, EffectModel, this);
                        var expressionResult = context.Evaluate(CompiledScript, expressionValue);

                        if (Property.PropertyType.TryConvertFromExpressionValue(expressionResult, rawValue, out var newValue))
                        {
                            if (Property is CompositionDependPropertyBase cp)
                            {
                                value = cp.CoerceValue(newValue, CompositionModel);
                            }
                            else
                            {
                                value = Property.CoerceValue(newValue);
                            }
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

            if (HasKeyFrames())
            {
                CreateKeyFrame(Property.DefaultValue);
            }
            else
            {
                CommitProperty(Property.DefaultValue, RawValue);
            }

            HistoryModel.EndGroup();
        }

        PropertyValueGroup? IPropertyObject.GetValues(double layerTime, bool withoutDisableProperty)
        {
            return null;
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

        public bool HasCompositionDependProperty()
        {
            return Property is CompositionDependPropertyBase;
        }

        public bool HasKeyFrames()
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

            ExpressionCode = data.ExpressionCode;
            UseExpression = data.UseExpression;

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
                RawValue = cp.CoerceValue(RawValue, CompositionModel);
                foreach (var k in oldKeyFrames.Select(k => new KeyFrame(k.Time, cp.CoerceValue(cp.PropertyType.DeserializeValue(k.Value), CompositionModel), k.EaseIn, k.EaseOut, k.InterpolationType, k.Id)))
                {
                    KeyFrames.Add(k);
                }
            }
            else
            {
                RawValue = Property.CoerceValue(RawValue);
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
            var oldValue = RawValue;

            LoadData(data);

            HistoryModel.Add(new OverwritePropertyHistoryCommand(this, oldKeyFrames, oldValue, [..KeyFrames], RawValue));
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
            if (e.PropertyName == nameof(LayerModel.SourceStartPoint))
            {
                SourceStartPoint = LayerModel?.SourceStartPoint ?? 0.0;
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
            ValueInvalidateByHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        ~PropertyModel()
        {
            CompiledScript?.Dispose();
        }
    }
}
