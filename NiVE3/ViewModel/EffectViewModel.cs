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
        private Guid effectId;
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public Guid EffectId
        {
            get { return effectId; }
            set { SetProperty(ref effectId, value); }
        }

        private string name = "";
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable;
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel> properties;
        public ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel> Properties
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

        WeakEventPublisher<EffectEnableChangeEventArgs> EffectEnableChangeRequestPublisher { get; } = new WeakEventPublisher<EffectEnableChangeEventArgs>();
        public event EventHandler<EffectEnableChangeEventArgs> EffectEnableChangeRequest
        {
            add { EffectEnableChangeRequestPublisher.Subscribe(value); }
            remove { EffectEnableChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

        public ICommand ChangeIsEnableCommand { get; }

        public ICommand SelectItemCommand { get; }

        EffectModel EffectModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public EffectViewModel(EffectModel effectModel)
#pragma warning restore CS8618
        {
            EffectModel = effectModel;

            ChangeIsEnableCommand = new DelegateCommand(() =>
            {
                EffectEnableChangeRequestPublisher.Publish(this, new EffectEnableChangeEventArgs(!IsEnable));
            });

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Effect, true, effect: this)));

            WiringModel();

            Properties = effectModel.Properties.CreateViewCollection(m =>
            {
                IInternalPropertyViewModel vm;
                if (m is PropertyGroupModel pg)
                {
                    vm = new PropertyGroupViewModel(pg);
                }
                else
                {
                    vm = new PropertyViewModel((PropertyModel)m);
                }
                vm.SelectItemChanged += Property_SelectItemChanged;
                return vm;
            });
        }

        public void DeSelect()
        {
            foreach (var p in Properties)
            {
                p.DeSelect();
            }
        }

        partial void WiringModel();

        private void Property_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, effect: this));
        }
    }
}
