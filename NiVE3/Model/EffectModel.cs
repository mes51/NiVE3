using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
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

        private ObservableCollection<IPropertyModel> properties = [];
        public ObservableCollection<IPropertyModel> Properties
        {
            get { return properties; }
            set { SetProperty(ref properties, value); }
        }

        public string EffectName => Metadata.Name;

        public bool IsDummyEffect => Metadata.IsDummyEffect;

        public EffectSupportedSource SupportedSource => Metadata.SupportedSource;

        public Guid EffectId { get; }

        public event EventHandler<EventArgs>? EffectUpdated;

        ExportLifetimeContext<IEffect> Effect { get; }

        IEffectMetadata Metadata { get; }

        HistoryModel HistoryModel { get; }

        public EffectModel(ExportLifetimeContext<IEffect> effect, IEffectMetadata metadata, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel) : this(effect, metadata, compositionModel, layerModel, historyModel, null) { }

        public EffectModel(ExportLifetimeContext<IEffect> effect, IEffectMetadata metadata, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? effectId)
        {
            Effect = effect;
            Metadata = metadata;
            Name = metadata.Name;
            HistoryModel = historyModel;
            EffectId = effectId ?? Guid.NewGuid();

            foreach (var p in effect.Value.GetProperties())
            {
                if (p is PropertyGroup)
                {
                    Properties.Add(new PropertyGroupModel(p, compositionModel, layerModel, this, historyModel));
                }
                else if (p is AppendableProperty)
                {
                    Properties.Add(new AppendablePropertyModel(p, compositionModel, layerModel, this, historyModel));
                }
                else
                {
                    Properties.Add(new PropertyModel(p, compositionModel, layerModel, this, historyModel));
                }
            }

            foreach (var p in Properties)
            {
                p.ValueUpdated += Property_ValueUpdated;
            }

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

        public NImage ProcessImage(NImage image, ROI roi, double layerTime, bool useGpu)
        {
            return Effect.Value.Process(image, roi, layerTime, Properties.ToArray(), useGpu);
        }

        public float[] ProcessAudio(float[] audio, double startTime)
        {
            return Effect.Value.Process(audio, startTime, Properties.ToArray());
        }

        public EffectData SaveData()
        {
            return new EffectData
            {
                EffectId = EffectId,
                EffectPluginId = Guid.Parse(Metadata.EffectUuid),
                Name = Name,
                IsEnable = IsEnable,
                Properties = Properties.Select(p => p.SaveData()).ToArray()
            };
        }

        public void LoadData(EffectData data)
        {
            Name = data.Name;
            IsEnable = data.IsEnable;
            foreach (var propertyData in data.Properties)
            {
                Properties.FirstOrDefault(p => p.PropertyId == propertyData.PropertyId)?.LoadData(propertyData);
            }
        }

        private void Property_ValueUpdated(object? sender, EventArgs e)
        {
            EffectUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void EffectModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            EffectUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Effect.Value.Dispose();
            Effect.Dispose();
        }
    }
}
