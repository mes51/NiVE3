using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Model;
using Prism.Commands;
using Prism.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Model.UI;
using NiVE3.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class MaskViewModel : BindableBase, INameEditableViewModel, IViewModelShortcutCommand
    {
        private Guid maskId;
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public Guid MaskId
        {
            get { return maskId; }
            set { SetProperty(ref maskId, value); }
        }

        private string name = "";
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable;
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        private bool isNameEditing;
        public bool IsNameEditing
        {
            get { return isNameEditing; }
            private set { SetProperty(ref isNameEditing, value); }
        }

        public PropertyGroupViewModel Properties { get; }

        WeakEventPublisher<MaskEnableChangeEventArgs> MaskEnableChangeRequestPublisher { get; } = new WeakEventPublisher<MaskEnableChangeEventArgs>();
        public event EventHandler<MaskEnableChangeEventArgs> MaskEnableChangeRequest
        {
            add { MaskEnableChangeRequestPublisher.Subscribe(value); }
            remove { MaskEnableChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<PropertyValueCommitedEventArgs> PropertyValueCommitedPublisher { get; } = new WeakEventPublisher<PropertyValueCommitedEventArgs>();
        public event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited
        {
            add { PropertyValueCommitedPublisher.Subscribe(value); }
            remove { PropertyValueCommitedPublisher.Unsubscribe(value); }
        }

        public ICommand ChangeIsEnableCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        MaskModel MaskModel { get; }

        string PrevName { get; set; } = "";

        public MaskViewModel(MaskModel maskModel, ViewStateModel viewState)
        {
            MaskModel = maskModel;
            Properties = new PropertyGroupViewModel(maskModel.Properties, viewState);

            ChangeIsEnableCommand = new DelegateCommand(() =>
            {
                MaskEnableChangeRequestPublisher.Publish(this, new MaskEnableChangeEventArgs(!IsEnable));
            });

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Mask, true, this)));

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                IsNameEditing = true;
                PrevName = Name;
            }, () => !IsNameEditing).ObservesProperty(() => IsNameEditing);

            EndEditNameCommand = new DelegateCommand<bool?>(commit =>
            {
                if ((commit ?? false) && !string.IsNullOrEmpty(Name))
                {
                    MaskModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                IsNameEditing = false;
            }, _ => IsNameEditing).ObservesProperty(() => IsNameEditing);

            DeleteCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                Properties.DeleteCommand.Execute(null);
            });

            CutCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                Properties.CutCommand.Execute(null);
            });

            CopyCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                Properties.CopyCommand.Execute(null);
            });

            PasteCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                Properties.PasteCommand.Execute(null);
            });

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => { });

            SelectAllCommand = new DelegateCommand<SelectItemType?>(_ => { });

            WiringModel();

            Properties.SelectItemChanged += Properties_SelectItemChanged;
            Properties.PropertyValueCommited += Properties_PropertyValueCommited;
        }

        public void DeSelect()
        {
            Properties.DeSelect();
        }

        partial void WiringModel();

        private void Properties_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }

        private void Properties_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
        }
    }
}
