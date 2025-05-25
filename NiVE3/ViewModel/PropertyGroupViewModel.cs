using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyGroupViewModel : BindableBase, IInternalPropertyViewModel, INameEditableViewModel
    {
        private string name = "";
        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable;
        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        private bool parentLayerIsLock;
        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public bool ParentLayerIsLock
        {
            get { return parentLayerIsLock; }
            set { SetProperty(ref parentLayerIsLock, value); }
        }

        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public bool UseEnableSwitch { get; set; }

        public PropertyViewState ViewState { get; }

        public ObservableCollection<KeyFrame>? KeyFrames => null;

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

        public object? CurrentTimeValue => null;

        public object? CurrentTimeRawValue
        {
            get => null;
            set { }
        }

        public bool IsEnableExpression => false;

        private bool isNameEditing;
        public bool IsNameEditing
        {
            get { return isNameEditing; }
            set { SetProperty(ref isNameEditing, value); }
        }

        public PropertyBase Property { get; }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<PropertyValueCommitedEventArgs> PropertyValueUpdatePublisher { get; } = new WeakEventPublisher<PropertyValueCommitedEventArgs>();
        public event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited
        {
            add { PropertyValueUpdatePublisher.Subscribe(value); }
            remove { PropertyValueUpdatePublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> IsEnableChangeRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> IsEnableChangeRequest
        {
            add { IsEnableChangeRequestPublisher.Subscribe(value); }
            remove { IsEnableChangeRequestPublisher.Unsubscribe(value); }
        }

        public ICommand BeginEditCommand => throw new NotImplementedException();

        public ICommand EndEditCommand => throw new NotImplementedException();

        public ICommand AbortEditCommand => throw new NotImplementedException();

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        public DelegateCommand<SelectItemType?> AddKeyFrameCommand { get; }

        public DelegateCommand<SelectItemType?> ResetPropertyCommand { get; }

        public DelegateCommand<SelectItemType?> SavePresetCommand { get; }

        public DelegateCommand<SelectItemType?> LoadPresetCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand AddKeyFrameToSelectedChildrenCommand { get; }

        public ICommand ResetSelectedChildrenCommand { get; }

        public ICommand CutSelectedChildrenCommand { get; }

        public ICommand CopySelectedChildrenCommand { get; }

        public ICommand PasteToSelectedChildrenCommand { get; }

        public ICommand DeleteSelectedChildrenCommand { get; }

        public ICommand DuplicateSelectedChildrenCommand { get; }

        public ICommand SavePropertyPresetCommand { get; }

        public ICommand LoadPropertyPresetCommand { get; }

        public ICommand PasteExpressionOnlyCommand { get; }

        public ICommand ChangeIsEnableCommand { get; }

        [NeedWire(nameof(PropertyGroupModel), IsOneWay = true)]
        public Guid InstanceId { get; set; }

        public bool IsRenameable { get; }

        private ObservableCollection<IInternalPropertyViewModel> selectedChildren = [];
        public ObservableCollection<IInternalPropertyViewModel> SelectedChildren
        {
            get { return selectedChildren; }
            set
            {
                selectedChildren.CollectionChanged -= SelectedChildren_CollectionChanged;
                value.CollectionChanged += SelectedChildren_CollectionChanged;
                SetProperty(ref selectedChildren, value);
            }
        }

        PropertyGroupModel PropertyGroupModel { get; }

        string PrevName { get; set; } = "";

        public PropertyGroupViewModel(PropertyGroupModel propertyGroupModel, ViewStateModel viewState) : this(propertyGroupModel, viewState, false) { }

        public PropertyGroupViewModel(PropertyGroupModel propertyGroupModel, ViewStateModel viewState, bool isRenameable)
        {
            SelectedChildren = [];
            PropertyGroupModel = propertyGroupModel;
            InstanceId = propertyGroupModel.InstanceId;
            Property = propertyGroupModel.Property;
            children = propertyGroupModel.Children.CreateViewCollection(m =>
            {
                var vm = InternalPropertyViewModel.CreateViewModel(m, viewState);
                vm.SelectItemChanged += Property_SelectItemChanged;
                vm.PropertyValueCommited += Property_PropertyValueCommited;
                return vm;
            });
            ViewState = propertyGroupModel.CreateState(this);
            IsRenameable = isRenameable;

            AddKeyFrameToSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                PropertyGroupModel.CreateKeyFrames([.. SelectedChildren.Select(c => c.Property.Id)]);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            ResetSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                PropertyGroupModel.ResetProperties([.. SelectedChildren.Select(c => c.Property.Id)]);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            CutSelectedChildrenCommand = new RequerySuggestedCommand(() =>
            {
                var targetChildren = SelectedChildren.OfType<PropertyViewModel>().Select(c => c.Property.Id).ToArray();
                if (targetChildren.Length < 1)
                {
                    return;
                }

                var data = PropertyGroupModel.CutChildrenKeyFrames(targetChildren);
                ClipboardUtil.SetData(data);
            }, () => SelectedChildren.Count > 0 && SelectedChildren.OfType<PropertyViewModel>().Any(p => p.HasKeyFrame));

            CopySelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var data = PropertyGroupModel.CopyChildrenProperty([.. SelectedChildren.Select(c => c.Property.Id)]);
                ClipboardUtil.SetData(data);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            PasteToSelectedChildrenCommand = new RequerySuggestedCommand(() =>
            {
                var propertyData = ClipboardUtil.GetData<PropertyData>();
                if (propertyData != null)
                {
                    PropertyGroupModel.PasteChildrenProperty(propertyData, [.. SelectedChildren.Select(c => c.Property.Id)]);
                }
            }, () =>
            {
                var type = ClipboardUtil.GetData<PropertyData>()?.Type;
                return type == CopyDataType.PropertyGroup || type == CopyDataType.AppendablePropertyChildren;
            });

            DeleteSelectedChildrenCommand = new RequerySuggestedCommand(() =>
            {
                var targetChildren = SelectedChildren.OfType<PropertyViewModel>().Select(c => c.Property.Id).ToArray();
                if (targetChildren.Length < 1)
                {
                    return;
                }

                PropertyGroupModel.DeleteChildrenKeyFrames(targetChildren);
            }, () => SelectedChildren.Count > 0 && SelectedChildren.OfType<PropertyViewModel>().Any(p => p.HasKeyFrame));

            DuplicateSelectedChildrenCommand = new DelegateCommand(() => { });

            SavePropertyPresetCommand = new RequerySuggestedCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var filePath = InternalPropertyViewModel.ShowPropertyPresetSaveDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                try
                {
                    PropertyGroupModel.SavePropertyPreset(filePath, [..SelectedChildren.Select(c => c.Property.Id)]);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => SelectedChildren.Count > 0);

            LoadPropertyPresetCommand = new RequerySuggestedCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var filePath = InternalPropertyViewModel.ShowPropertyPresetOpenDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                try
                {
                    PropertyGroupModel.LoadPropertyPreset(filePath, [..SelectedChildren.Select(c => c.Property.Id)]);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => SelectedChildren.Count > 0);

            DeleteCommand = new DelegateCommand<SelectItemType?>(_ => DeleteSelectedChildrenCommand.Execute(null));

            CutCommand = new DelegateCommand<SelectItemType?>(_ => CutSelectedChildrenCommand.Execute(null));

            CopyCommand = new DelegateCommand<SelectItemType?>(_ => CopySelectedChildrenCommand.Execute(null));

            PasteCommand = new DelegateCommand<SelectItemType?>(_ => PasteToSelectedChildrenCommand.Execute(null));

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => { });

            SelectAllCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                DeSelect();
                foreach (var child in Children)
                {
                    SelectedChildren.Add(child);
                }
            });

            AddKeyFrameCommand = new DelegateCommand<SelectItemType?>(_ => AddKeyFrameToSelectedChildrenCommand.Execute(null));

            ResetPropertyCommand = new DelegateCommand<SelectItemType?>(_ => ResetSelectedChildrenCommand.Execute(null));

            SavePresetCommand = new DelegateCommand<SelectItemType?>(_ => SavePropertyPresetCommand.Execute(null));

            LoadPresetCommand = new DelegateCommand<SelectItemType?>(_ => LoadPropertyPresetCommand.Execute(null));

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                PrevName = Name;
                IsNameEditing = true;
            }, () => !ParentLayerIsLock && IsRenameable && !IsNameEditing)
                .ObservesProperty(() => ParentLayerIsLock)
                .ObservesProperty(() => IsNameEditing);

            EndEditNameCommand = new DelegateCommand<bool?>(commit =>
            {
                if (commit ?? false)
                {
                    PropertyGroupModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                IsNameEditing = false;
            }, _ => IsRenameable && IsNameEditing).ObservesProperty(() => IsNameEditing);

            SelectItemCommand = new DelegateCommand(() =>
            {
                DeSelect();
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Property, true, this));
            });

            PasteExpressionOnlyCommand = new RequerySuggestedCommand(() =>
            {
                var propertyData = ClipboardUtil.GetData<PropertyData>();
                if (propertyData != null)
                {
                    PropertyGroupModel.PasteExpressionOnly(propertyData, [..SelectedChildren.Select(c => c.Property.Id)]);
                }
            }, () =>
            {
                var type = ClipboardUtil.GetData<PropertyData>()?.Type;
                return type == CopyDataType.PropertyGroup || type == CopyDataType.AppendablePropertyChildren;
            });

            ChangeIsEnableCommand = new DelegateCommand(() =>
            {
                IsEnableChangeRequestPublisher.Publish(this, EventArgs.Empty);
            });

            WiringModel();
        }

        public void DeSelect()
        {
            foreach (var p in Children)
            {
                p.DeSelect();
            }
            SelectedChildren.Clear();
        }

        partial void WiringModel();

        private void Property_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            if (PropertyGroupModel.ParentLayerIsLock)
            {
                DeSelect();
            }
            else if (e.OriginalSender is IInternalPropertyViewModel property && Children.Contains(property))
            {
                foreach (var child in Children)
                {
                    if (child is PropertyViewModel childProperty && SelectedChildren.Contains(child))
                    {
                        childProperty.SelectAllKeyFrames();
                    }
                    else
                    {
                        child.DeSelect();
                    }
                }
            }
            else
            {
                var exclude = e.ObjectHierarchy.OfType<IInternalPropertyViewModel>().FirstOrDefault(Children.Contains);
                foreach (var notSelected in Children.Where(c => c != exclude))
                {
                    notSelected.DeSelect();
                    SelectedChildren.Remove(notSelected);
                }
                if (exclude != null && !SelectedChildren.Contains(exclude))
                {
                    SelectedChildren.Add(exclude);
                }
            }
        }

        private void Property_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueUpdatePublisher.Publish(sender, new PropertyValueCommitedEventArgs(e, this));
        }

        private void SelectedChildren_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var child in Children)
                {
                    child.DeSelect();
                }
            }
            else
            {
                foreach (var old in e.OldItems?.OfType<IInternalPropertyViewModel>() ?? [])
                {
                    old.DeSelect();
                }
            }
        }
    }
}
