using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Model;
using NiVE3.Mvvm;
using System.Windows.Input;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class EffectViewModel : BindableBase
    {
        private string name = "";
        [NeedWire(nameof(EffectModel))]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable;
        [NeedWire(nameof(EffectModel))]
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private ObservableCollectionView<PropertyModel, PropertyViewModel> properties;
        public ObservableCollectionView<PropertyModel, PropertyViewModel> Properties
        {
            get { return properties; }
            set { SetProperty(ref properties, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        public ICommand ChangeIsEnableCommand { get; }

        EffectModel EffectModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public EffectViewModel(EffectModel effectModel)
#pragma warning restore CS8618
        {
            EffectModel = effectModel;

            ChangeIsEnableCommand = new DelegateCommand(() => IsEnable = !IsEnable);

            WiringModel();

            Properties = effectModel.Properties.CreateViewCollection(m => new PropertyViewModel(m));
        }

        partial void WiringModel();
    }
}
