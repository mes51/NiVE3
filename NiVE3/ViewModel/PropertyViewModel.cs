using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    interface IInternalPropertyViewModel : IPropertyViewModel
    {
        string DisplayName { get; }

        ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children { get; }

        PropertyControlBase CreateControl();
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyViewModel : BindableBase, IInternalPropertyViewModel
    {
        private string displayName = "";
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        public ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children => null;

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

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyGroupViewModel : BindableBase, IInternalPropertyViewModel
    {
        private string displayName = "";
        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        private ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel> children;
        public ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        public object? Value
        {
            get => null;
            set { }
        }

        public PropertyBase Property { get; }

        public ICommand BeginEditCommand => throw new NotImplementedException();

        public ICommand EndEditCommand => throw new NotImplementedException();

        public ICommand AbortEditCommand => throw new NotImplementedException();

        PropertyGroupModel PropertyGroupModel { get; }

        public PropertyGroupViewModel(PropertyGroupModel propertyGroupModel)
        {
            PropertyGroupModel = propertyGroupModel;
            Property = propertyGroupModel.Property;
            children = propertyGroupModel.Children.CreateViewCollection<IPropertyModel, IInternalPropertyViewModel>(m =>
            {
                if (m is PropertyGroupModel pg)
                {
                    return new PropertyGroupViewModel(pg);
                }
                else
                {
                    return new PropertyViewModel((PropertyModel)m);
                }
            });

            WiringModel();
        }

        public PropertyControlBase CreateControl()
        {
            throw new NotImplementedException();
        }

        partial void WiringModel();
    }
}
