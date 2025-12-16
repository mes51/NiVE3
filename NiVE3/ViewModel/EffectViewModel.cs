using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.Model;
using NiVE3.Mvvm;
using System.Windows.Input;
using Prism.Commands;
using NiVE3.Model.UI;

namespace NiVE3.ViewModel
{
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class EffectViewModel : BindableBase, INameEditableViewModel, IViewModelShortcutCommand
    {
        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial Guid EffectId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial string Comment { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial bool IsEnable { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial bool ParentLayerIsLock { get; set; }

        [ReactiveProperty]
        public partial bool IsExpanded { get; set; }

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

        WeakEventPublisher<PropertyValueCommitedEventArgs> PropertyValueCommitedPublisher { get; } = new WeakEventPublisher<PropertyValueCommitedEventArgs>();
        public event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited
        {
            add { PropertyValueCommitedPublisher.Subscribe(value); }
            remove { PropertyValueCommitedPublisher.Unsubscribe(value); }
        }

        [ReactiveProperty]
        public partial bool IsNameEditing { get; private set; }

        [ReactiveProperty]
        public partial bool IsCommentEditing { get; set; }

        public PropertyGroupViewModel Properties { get; }

        public ICommand ChangeIsEnableCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        EffectModel EffectModel { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        public EffectViewModel(EffectModel effectModel, ViewStateModel viewState)
        {
            EffectModel = effectModel;
            Properties = new PropertyGroupViewModel(effectModel.Properties, viewState);

            ChangeIsEnableCommand = new DelegateCommand(() =>
            {
                EffectEnableChangeRequestPublisher.Publish(this, new EffectEnableChangeEventArgs(!IsEnable));
            });

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Effect, true, this)));

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                IsNameEditing = true;
                PrevName = Name;
            }, () => !ParentLayerIsLock && !IsNameEditing && !IsCommentEditing)
                .ObservesProperty(() => ParentLayerIsLock)
                .ObservesProperty(() => IsNameEditing)
                .ObservesProperty(() => IsCommentEditing);

            BeginEditCommentCommand = new DelegateCommand(() =>
            {
                IsCommentEditing = true;
                PrevComment = Comment;
            }, () => !ParentLayerIsLock && !IsNameEditing && !IsCommentEditing)
                .ObservesProperty(() => ParentLayerIsLock)
                .ObservesProperty(() => IsNameEditing)
                .ObservesProperty(() => IsCommentEditing);

            EndEditNameCommand = new DelegateCommand<bool?>(commit =>
            {
                if ((commit ?? false) && !string.IsNullOrEmpty(Name))
                {
                    EffectModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                IsNameEditing = false;
            }, _ => IsNameEditing).ObservesProperty(() => IsNameEditing);

            EndEditCommentCommand = new DelegateCommand<bool?>(commit =>
            {
                if (commit ?? false)
                {
                    EffectModel.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                IsCommentEditing = false;
            }, _ => IsCommentEditing).ObservesProperty(() => IsCommentEditing);

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

            Properties.SelectItemChanged += Property_SelectItemChanged;
            Properties.PropertyValueCommited += Property_PropertyValueCommited;
        }

        public void DeSelect()
        {
            Properties.DeSelect();
            Properties.ClearInteractionState();
        }

        partial void WiringModel();

        private void Property_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
        }

        private void Property_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }
    }
}
