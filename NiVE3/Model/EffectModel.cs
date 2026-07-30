using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Exceptions;
using NiVE3.Extension;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.Model
{
    [UseReactiveProperty]
    partial class EffectModel : BindableBase, IDisposable, IEffectObject
    {
        [ReactiveProperty]
        public partial string Name { get; set; }

        [ReactiveProperty]
        public partial string Comment { get; set; } = "";

        [ReactiveProperty]
        public partial bool IsEnable { get; set; } = true;

        [ReactiveProperty]
        public partial bool ParentLayerIsLock { get; set; }

        public PropertyGroupModel Properties { get; }

        public string EffectName => Metadata.Name;

        public bool IsRenderEveryFrame => Metadata.IsRenderEveryFrame || UseCompositionCamera; // TODO: アクティブカメラが変わったときのみ再レンダリングするようにする

        public bool IsDummyEffect => Metadata.IsDummyEffect;

        public bool UseCompositionCamera => Metadata.UseCompositionCamera;

        public EffectSupportedSource SupportedSource => Metadata.SupportedSource;

        public Guid EffectId { get; }

        public Guid EffectPluginId => Guid.Parse(Metadata.EffectUuid);

        public event EventHandler<EventArgs>? EffectUpdated;

        IEffectHandle Effect { get; }

        IEffectMetadata Metadata { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        HistoryModel HistoryModel { get; }

        string EffectPropertyGroupId => $"{EffectName}_Properties";

        bool IsSupportGpu { get; }

        public EffectModel(IEffectHandle effect, IEffectMetadata metadata, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel) : this(effect, metadata, projectModel, compositionModel, layerModel, historyModel, null) { }

        public EffectModel(IEffectHandle effect, IEffectMetadata metadata, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? effectId)
        {
            Effect = effect;
            Metadata = metadata;
            Name = metadata.Name;
            HistoryModel = historyModel;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectId = effectId ?? Guid.NewGuid();
            Properties = new PropertyGroupModel(new PropertyGroup(EffectPropertyGroupId, "", effect.Value.GetProperties(new Int32Size(layerModel.SourceWidth, layerModel.SourceHeight))), EffectId.ToInt128(), projectModel, compositionModel, layerModel, this, null, historyModel, false);
            IsSupportGpu = metadata.IsSupportGpu;

            LayerModel.PropertyChanged += LayerModel_PropertyChanged;
            Properties.ValueUpdated += Property_ValueUpdated;
            Properties.ValueCommited += Properties_ValueCommited;
            PropertyChanged += EffectModel_PropertyChanged;

            if (effect.Value is IPropertyEditAwareEffect editAwareEffect)
            {
                editAwareEffect.PropertyValuesWriteback += Effect_PropertyValuesWriteback;
                SetEditSessionSubscription(Properties.Children, true);
            }
        }

        // 編集セッション (ドラッグ操作等) の進行数。セッション中の書き戻しは確定まで保留する
        int ActiveEditSessions { get; set; }

        // 編集中止時の値復元中など、エフェクトへの編集通知を一時的に抑止するためのフラグ
        bool SuppressEditNotify { get; set; }

        // セッション中に書き戻された値 (プロパティID → モデル・セッション開始時の値・最新値)
        Dictionary<string, (PropertyModel Model, object? FirstValue, object? LatestValue)> PendingSessionWritebacks { get; } = [];

        void SetEditSessionSubscription(IEnumerable<IPropertyModel> models, bool subscribe)
        {
            foreach (var model in models)
            {
                if (model is PropertyModel propertyModel)
                {
                    if (subscribe)
                    {
                        propertyModel.EditSessionBegan += PropertyModel_EditSessionBegan;
                        propertyModel.EditSessionEnded += PropertyModel_EditSessionEnded;
                        propertyModel.EditSessionAborted += PropertyModel_EditSessionAborted;
                    }
                    else
                    {
                        propertyModel.EditSessionBegan -= PropertyModel_EditSessionBegan;
                        propertyModel.EditSessionEnded -= PropertyModel_EditSessionEnded;
                        propertyModel.EditSessionAborted -= PropertyModel_EditSessionAborted;
                    }
                }
                if (model.Children != null)
                {
                    SetEditSessionSubscription(model.Children, subscribe);
                }
            }
        }

        private void PropertyModel_EditSessionBegan(object? sender, EventArgs e)
        {
            ActiveEditSessions++;
        }

        private void PropertyModel_EditSessionEnded(object? sender, EventArgs e)
        {
            if (ActiveEditSessions > 0 && --ActiveEditSessions == 0)
            {
                FlushSessionWritebacks(true);
            }
        }

        private void PropertyModel_EditSessionAborted(object? sender, EventArgs e)
        {
            if (ActiveEditSessions > 0 && --ActiveEditSessions == 0)
            {
                FlushSessionWritebacks(false);
            }
        }

        void FlushSessionWritebacks(bool commit)
        {
            if (PendingSessionWritebacks.Count < 1)
            {
                return;
            }
            var pending = PendingSessionWritebacks.Values.ToArray();
            PendingSessionWritebacks.Clear();

            if (commit)
            {
                // セッション全体の変更を 1 つの履歴 (グループ) として確定する
                var targets = pending.Where(p => !Equals(p.FirstValue, p.LatestValue)).ToArray();
                if (targets.Length == 1)
                {
                    targets[0].Model.CommitProperty(targets[0].LatestValue, targets[0].FirstValue);
                }
                else if (targets.Length > 1)
                {
                    HistoryModel.BeginGroup(EffectName);
                    try
                    {
                        foreach (var (model, firstValue, latestValue) in targets)
                        {
                            model.CommitProperty(latestValue, firstValue);
                        }
                    }
                    finally
                    {
                        HistoryModel.EndGroup();
                    }
                }
            }
            else
            {
                // 中止: セッション開始時の値へ黙って戻す (復元中の編集通知は抑止し、復元後にまとめて通知する)
                SuppressEditNotify = true;
                try
                {
                    foreach (var (model, firstValue, _) in pending)
                    {
                        model.UpdateUncommitedRawValue(firstValue);
                    }
                }
                finally
                {
                    SuppressEditNotify = false;
                }
                (Effect.Value as IPropertyEditAwareEffect)?.OnPropertyValuesRestored(Properties.Children.ToArray());
            }
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

        public void ChangeComment(string comment)
        {
            if (comment != Comment)
            {
                var oldComment = Comment;
                Comment = comment;

                HistoryModel.Add(new ChangeCommentHistoryCommand(this, oldComment, comment));
            }
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime)
        {
            return Effect.Value.CalcRoi(baseRoi, downSamplingRateX, downSamplingRateY, layerTime, Properties.Children.ToArray(), CompositionModel, LayerModel);
        }

        public NImage ProcessImage(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, bool useGpu)
        {
            try
            {
                return Effect.Value.Process(image, roi, downSamplingRateX, downSamplingRateY, layerTime, Properties.Children.ToArray(), CompositionModel, LayerModel, useGpu && IsSupportGpu);
            }
            catch (Exception ex)
            {
                if (useGpu)
                {
                    throw new GPUException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public float[] ProcessAudio(float[] audio, Time startTime)
        {
            return Effect.Value.Process(audio, startTime, Properties.Children.ToArray(), CompositionModel, LayerModel);
        }

        public EffectData SaveData()
        {
            return new EffectData
            {
                EffectId = EffectId,
                EffectPluginId = EffectPluginId,
                Name = Name,
                Comment = Comment,
                IsEnable = IsEnable,
                Properties = Properties.SaveData()
            };
        }

        public void LoadData(EffectData data)
        {
            Name = data.Name;
            Comment = data.Comment;
            IsEnable = data.IsEnable;
            if (data.Properties != null)
            {
                Properties.LoadData(data.Properties);
            }
        }

        public void CoerceProperties()
        {
            Properties.CoerceValues();
        }

        public void OverwriteEffect(EffectData data)
        {
            if (data.EffectPluginId != EffectPluginId)
            {
                return;
            }

            var oldData = SaveData();
            LoadData(data);

            HistoryModel.Add(new OverwriteEffectHistoryCommand(this, oldData, data));
        }

        public void CalcPropertyHash(Time layerTime, Time globalTime, XxHash3 hash)
        {
            hash.Append(EffectPluginId);
            hash.Append(Name);
            hash.Append(Comment);
            hash.Append(IsEnable);
            Properties.GetValues(layerTime, globalTime).CalcHash(hash);
        }

        public void UpdateCompositionDependProperties()
        {
            Properties.UpdateValueByCompositionStateChanged();
        }

        public void UpdateLayerDependProperties()
        {
            Properties.UpdateValueByLayerStateChanged();
        }

        public void ReplaceLayerDependPropertiesEffectId(Dictionary<Guid, Guid> effectIdMap)
        {
            Properties.UpdateValueByReplacedEffectId(effectIdMap);
        }

        public void ReplaceLayerDependPropertiesMaskId(Dictionary<Guid, Guid> maskIdMap)
        {
            Properties.UpdateValueByReplacedMaskId(maskIdMap);
        }

        public void ReplaceCompositionDependPropertiesLayerId(Dictionary<Guid, Guid> layerIdMap)
        {
            Properties.UpdateValueByReplacedLayerId(layerIdMap);
        }

        public bool ClearExpressionError()
        {
            return Properties.ClearExpressionError();
        }

        public bool HasCompositionDependProperty()
        {
            return Properties.HasCompositionDependProperty();
        }

        public bool PropertyIsChangeableByTime()
        {
            return Properties.IsChangeableByTime();
        }

        public bool IsAlive()
        {
            return LayerModel.IsAlive(this);
        }

        public static (Guid oldId, Guid newId) ConvertDataForImport(EffectData effectData)
        {
            var oldId = effectData.EffectId;
            effectData.EffectId = Guid.NewGuid();

            return (oldId, effectData.EffectId);
        }

        private void LayerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerModel.IsLock))
            {
                ParentLayerIsLock = LayerModel.IsLock;
            }
        }

        // Undo/Redo 完了後の復元通知を予約済みかどうか (複数プロパティの復元を 1 回の通知にまとめる)
        bool RestoreNotifyScheduled { get; set; }

        private void Property_ValueUpdated(object? sender, EventArgs e)
        {
            // 値の編集を通知するエフェクト (OpenFX アダプタなど) へ変更を伝える
            // Undo/Redo や書き戻しの反映中は通知しない (プラグインが再度値を変更して履歴が壊れるのを防ぐ)
            if (!HistoryModel.IsChanging && !SuppressEditNotify)
            {
                (Effect.Value as IPropertyEditAwareEffect)?.OnPropertyValuesEdited(Properties.Children.ToArray());
            }
            else if (Effect.Value is IPropertyEditAwareEffect editAwareEffect && !RestoreNotifyScheduled)
            {
                // Undo/Redo による値の復元後にプラグインへ通知し、表示/有効状態などの内部状態を復元させる
                // (復元処理の完了後に実行されるようキューへ積む)
                var application = System.Windows.Application.Current;
                if (application != null)
                {
                    RestoreNotifyScheduled = true;
                    application.Dispatcher.BeginInvoke(() =>
                    {
                        RestoreNotifyScheduled = false;
                        if (IsAlive())
                        {
                            editAwareEffect.OnPropertyValuesRestored(Properties.Children.ToArray());
                        }
                    });
                }
            }

            EffectUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Effect_PropertyValuesWriteback(object? sender, PropertyValuesWritebackEventArgs e)
        {
            if (e.IsUserAction)
            {
                // ユーザー操作起因 (編集やボタンの InstanceChanged 完了後)。
                // プラグインのアクションは完了しているため同期的に反映し、Undo できるように履歴へ積む
                var application = System.Windows.Application.Current;
                if (application != null && !application.Dispatcher.CheckAccess())
                {
                    application.Dispatcher.BeginInvoke(() => ApplyWritebacksWithHistory(e.Values));
                }
                else
                {
                    ApplyWritebacksWithHistory(e.Values);
                }
            }
            else
            {
                // レンダリング中のステータス表示更新など: 履歴には積まず、キュー経由で非同期に反映する
                var application = System.Windows.Application.Current;
                if (application != null)
                {
                    application.Dispatcher.BeginInvoke(() => ApplyWritebacksSilently(e.Values));
                }
                else
                {
                    ApplyWritebacksSilently(e.Values);
                }
            }
        }

        void ApplyWritebacksWithHistory(IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            var targets = new List<(PropertyModel Model, object? NewValue, object? PrevValue)>();
            foreach (var (propertyId, value) in values)
            {
                if (FindPropertyModel(Properties.Children, propertyId) is PropertyModel propertyModel)
                {
                    var newValue = propertyModel.Property.CoerceValue(value);

                    // 非永続 (表示専用) のプロパティは履歴に積まず表示のみ更新する (ライセンス状態など)
                    if (!propertyModel.Property.IsPersistent)
                    {
                        propertyModel.UpdateUncommitedRawValue(newValue);
                        continue;
                    }

                    // キーフレームがない場合、GetValue は時間によらず現在の値を返す
                    // (キーフレームがある場合、CommitProperty は prevValue を使用せずキーフレームを作成する)
                    var prevValue = propertyModel.GetValue(Time.Zero, Time.Zero);
                    if (!Equals(newValue, prevValue))
                    {
                        targets.Add((propertyModel, newValue, prevValue));
                    }
                }
            }

            if (targets.Count < 1)
            {
                return;
            }

            // 編集セッション (ドラッグ操作等) 中は履歴を確定せず保留し、ライブ表示のみ更新する
            // (ドラッグの 1 ティックごとに履歴が積まれるのを防ぎ、EndEdit で 1 つの履歴にまとめる)
            if (ActiveEditSessions > 0)
            {
                foreach (var (model, newValue, prevValue) in targets)
                {
                    if (PendingSessionWritebacks.TryGetValue(model.Property.Id, out var pending))
                    {
                        PendingSessionWritebacks[model.Property.Id] = (model, pending.FirstValue, newValue);
                    }
                    else
                    {
                        PendingSessionWritebacks[model.Property.Id] = (model, prevValue, newValue);
                    }
                    model.UpdateUncommitedRawValue(newValue);
                }
                return;
            }

            if (targets.Count == 1)
            {
                targets[0].Model.CommitProperty(targets[0].NewValue, targets[0].PrevValue);
            }
            else
            {
                HistoryModel.BeginGroup(EffectName);
                try
                {
                    foreach (var (model, newValue, prevValue) in targets)
                    {
                        model.CommitProperty(newValue, prevValue);
                    }
                }
                finally
                {
                    HistoryModel.EndGroup();
                }
            }
        }

        void ApplyWritebacksSilently(IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            foreach (var (propertyId, value) in values)
            {
                if (FindPropertyModel(Properties.Children, propertyId) is PropertyModel propertyModel)
                {
                    // 値が同じ場合は ReactiveProperty 側で無視されるため、再レンダリングのループにはならない
                    propertyModel.UpdateUncommitedRawValue(propertyModel.Property.CoerceValue(value));
                }
            }
        }

        static IPropertyModel? FindPropertyModel(IEnumerable<IPropertyModel> models, string propertyId)
        {
            foreach (var model in models)
            {
                if (model.Property.Id == propertyId)
                {
                    return model;
                }
                if (model.Children != null)
                {
                    var found = FindPropertyModel(model.Children, propertyId);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private void Properties_ValueCommited(object? sender, EventArgs e)
        {
            EffectUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void EffectModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            EffectUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (Effect.Value is IPropertyEditAwareEffect editAwareEffect)
            {
                editAwareEffect.PropertyValuesWriteback -= Effect_PropertyValuesWriteback;
                SetEditSessionSubscription(Properties.Children, false);
            }
            Effect.Dispose();
        }
    }
}
