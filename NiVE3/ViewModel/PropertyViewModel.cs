using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyViewModel : BindableBase, IPropertyViewModel
    {
        private string displayName = "";
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        private object? _value; // valueキーワードと被るため仕方なしでアンダーバーをつける
        [NeedWire(nameof(PropertyModel))]
        public object? Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public PropertyBase Property { get; }

        public ICommand BeginEditCommand { get; }

        public ICommand EndEditCommand { get; }

        public ICommand AbortEditCommand { get; }

        PropertyModel PropertyModel { get; }

        object? PrevValue { get; set; }

        bool IsEditing { get; set; }

        public PropertyViewModel(PropertyModel propertyModel)
        {
            PropertyModel = propertyModel;
            Property = propertyModel.Property;

            BeginEditCommand = new RequerySuggestedCommand(() =>
            {
                PrevValue = Value;
                IsEditing = true;
            }, () => !IsEditing);

            EndEditCommand = new RequerySuggestedCommand(() =>
            {
                PropertyModel.CommitProperty(PrevValue);
                IsEditing = false;
            }, () => IsEditing);

            AbortEditCommand = new RequerySuggestedCommand(() =>
            {
                Value = PrevValue;
                IsEditing = false;
            }, () => IsEditing);

            WiringModel();
        }

        public PropertyControlBase CreateControl()
        {
            return PropertyModel.CreateControl(this);
        }

        partial void WiringModel();
    }
}
