using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
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

        private ObservableCollection<PropertyModel> properties = new ObservableCollection<PropertyModel>();
        public ObservableCollection<PropertyModel> Properties
        {
            get { return properties; }
            set { SetProperty(ref properties, value); }
        }

        public string EffectName => Metadata.Name;

        public bool IsDummyEffect => Metadata.IsDummyEffect;

        public Guid EffectId { get; }

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
                Properties.Add(new PropertyModel(p, compositionModel, layerModel, this, historyModel));
            }
        }

        public void Dispose()
        {
            Effect.Value.Dispose();
            Effect.Dispose();
        }
    }
}
