using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Project;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class EffectModel : BindableBase, IDisposable, IEffectObject
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable = true;
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private ObservableCollection<IPropertyModel> properties = new ObservableCollection<IPropertyModel>();
        public ObservableCollection<IPropertyModel> Properties
        {
            get { return properties; }
            set { SetProperty(ref properties, value); }
        }

        public string EffectName => Metadata.Name;

        public bool IsDummyEffect => Metadata.IsDummyEffect;

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

        public NImage Process(NImage image, ROI roi, double layerTime)
        {
            var propertyValues = new Dictionary<string, object?>();
            foreach (var p in Properties)
            {
                if (p is PropertyGroupModel pg)
                {
                    propertyValues.Add(pg.Id, pg.GetPropertyValueGroup(layerTime));
                }
                else if (p is PropertyModel pp)
                {
                    propertyValues.Add(pp.Id, pp.GetValue(layerTime));
                }
            }

            return Effect.Value.Process(image, roi, layerTime, new PropertyValueGroup(propertyValues));
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
