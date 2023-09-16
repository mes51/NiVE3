using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class PropertyModel : BindableBase
    {
        private string id = "";
        public string Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        private string displayName = "";
        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        private object? _value = null; // valueキーワードと被るため仕方なしでアンダーバーをつける
        public object? Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public PropertyBase Property { get; }

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
            DisplayName = property.DisplayName;
            Value = property.DefaultValue;
        }

        public void CommitProperty(object? prevValue)
        {
            HistoryModel.Add(new ValueChangeHistoryCommand(this, prevValue, Value));
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            return Property.CreateControl(CompositionModel, LayerModel, EffectModel, viewModel);
        }
    }
}
