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
using NiVE3.UI.Command;
using System.Collections.ObjectModel;
using NiVE3.Data.Json.Project;
using NiVE3.Util;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class EffectViewModel : BindableBase, INameEditableViewModel, IViewModelShortcutCommand
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

        private string comment = "";
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private bool isEnable;
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
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

        private bool isNameEditing;
        public bool IsNameEditing
        {
            get { return isNameEditing; }
            private set { SetProperty(ref isNameEditing, value); }
        }

        private bool isCommentEditing;
        public bool IsCommentEditing
        {
            get { return isCommentEditing; }
            set { SetProperty(ref isCommentEditing, value); }
        }

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

        EffectModel EffectModel { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public EffectViewModel(EffectModel effectModel)
#pragma warning restore CS8618
        {
            EffectModel = effectModel;
            Properties = new PropertyGroupViewModel(effectModel.Properties);

            ChangeIsEnableCommand = new DelegateCommand(() =>
            {
                EffectEnableChangeRequestPublisher.Publish(this, new EffectEnableChangeEventArgs(!IsEnable));
            });

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Effect, true, this)));

            BeginEditNameCommand = new RequerySuggestedCommand(() =>
            {
                IsNameEditing = true;
                PrevName = Name;
            }, () => !IsNameEditing && !IsCommentEditing);

            BeginEditCommentCommand = new RequerySuggestedCommand(() =>
            {
                IsCommentEditing = true;
                PrevComment = Comment;
            }, () => !IsNameEditing && !IsCommentEditing);

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit && !string.IsNullOrEmpty(Name))
                {
                    EffectModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                IsNameEditing = false;
            }, _ => IsNameEditing);

            EndEditCommentCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit)
                {
                    EffectModel.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                IsCommentEditing = false;
            }, _ => IsCommentEditing);

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

            WiringModel();

            Properties.SelectItemChanged += Property_SelectItemChanged;
            Properties.PropertyValueCommited += Property_PropertyValueCommited;
        }

        public void DeSelect()
        {
            Properties.DeSelect();
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
