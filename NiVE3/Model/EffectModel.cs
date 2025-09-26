using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class EffectModel : BindableBase, IDisposable, IEffectObject
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private bool isEnable = true;
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private bool parentLayerIsLock;
        public bool ParentLayerIsLock
        {
            get { return parentLayerIsLock; }
            set { SetProperty(ref parentLayerIsLock, value); }
        }

        public PropertyGroupModel Properties { get; }

        public string EffectName => Metadata.Name;

        public bool IsRenderEveryFrame => Metadata.IsRenderEveryFrame || UseCompositionCamera; // TODO: アクティブカメラが変わったときのみ再レンダリングするようにする

        public bool IsDummyEffect => Metadata.IsDummyEffect;

        public bool UseCompositionCamera => Metadata.UseCompositionCamera;

        public EffectSupportedSource SupportedSource => Metadata.SupportedSource;

        public Guid EffectId { get; }

        public Guid EffectPluginId => Guid.Parse(Metadata.EffectUuid);

        public event EventHandler<EventArgs>? EffectUpdated;

        ExportLifetimeContext<IEffect> Effect { get; }

        IEffectMetadata Metadata { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        HistoryModel HistoryModel { get; }

        string EffectPropertyGroupId => $"{EffectName}_Properties";

        bool IsSupportGpu { get; }

        public EffectModel(ExportLifetimeContext<IEffect> effect, IEffectMetadata metadata, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel) : this(effect, metadata, projectModel, compositionModel, layerModel, historyModel, null) { }

        public EffectModel(ExportLifetimeContext<IEffect> effect, IEffectMetadata metadata, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? effectId)
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

        private void Property_ValueUpdated(object? sender, EventArgs e)
        {
            EffectUpdated?.Invoke(this, EventArgs.Empty);
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
            Effect.Dispose();
        }
    }
}
