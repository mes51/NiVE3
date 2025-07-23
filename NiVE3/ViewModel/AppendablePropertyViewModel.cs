using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using Prism.Commands;
using Prism.Mvvm;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Property;
using NiVE3.UI.Command;
using NiVE3.View.Primitive;
using NiVE3.Util;
using NiVE3.Data.Json.Project;
using NiVE3.Data.Clipboard;
using NiVE3.Model.UI;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class AppendablePropertyViewModel : BindableBase, IInternalPropertyViewModel, IDropTarget, INameEditableParentViewModel
    {
        public bool IsEnable => true;

        public string Name { get; }

        public PropertyViewState ViewState { get; }

        private bool parentLayerIsLock;
        [NeedWire(nameof(AppendablePropertyModel), IsOneWay = true)]
        public bool ParentLayerIsLock
        {
            get { return parentLayerIsLock; }
            set { SetProperty(ref parentLayerIsLock, value); }
        }

        public ObservableCollection<KeyFrame>? KeyFrames => throw new NotImplementedException();

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

        public ICommand BeginEditCommand => throw new NotImplementedException();

        public ICommand EndEditCommand => throw new NotImplementedException();

        public ICommand AbortEditCommand => throw new NotImplementedException();

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

        public ICommand AppendItemCommand { get; }

        public AppendablePropertyItem[] Items => ((AppendableProperty)Property).Items;

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

        public INameEditableViewModel? TargetChild => SelectedChildren.FirstOrDefault() as INameEditableViewModel;

        AppendablePropertyModel AppendablePropertyModel { get; }

        public AppendablePropertyViewModel(AppendablePropertyModel appendablePropertyModel, ViewStateModel viewState)
        {
            SelectedChildren = [];
            AppendablePropertyModel = appendablePropertyModel;
            Property = appendablePropertyModel.Property;
            children = appendablePropertyModel.Children.CreateViewCollection(m =>
            {
                var vm = new PropertyGroupViewModel((PropertyGroupModel)m, viewState, true);
                vm.SelectItemChanged += Property_SelectItemChanged;
                vm.PropertyValueCommited += Property_PropertyValueCommited;
                vm.IsEnableChangeRequest += Property_IsEnableChangeRequest;
                return (IInternalPropertyViewModel)vm;
            });
            Name = appendablePropertyModel.Name;
            ViewState = appendablePropertyModel.CreateState(this);

            SelectItemCommand = new DelegateCommand(() =>
            {
                DeSelect();
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.PropertyGroup, true, this));
            });

            AddKeyFrameToSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                AppendablePropertyModel.CreateKeyFrames([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId)]);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            ResetSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                AppendablePropertyModel.ResetProperties([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId)]);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            CutSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var data = AppendablePropertyModel.CutChildren([.. SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId)]);
                ClipboardUtil.SetData(data);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            CopySelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var data = AppendablePropertyModel.CopyChildrenProperty([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(p => p.InstanceId)]);
                ClipboardUtil.SetData(data);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            PasteToSelectedChildrenCommand = new RequerySuggestedCommand(() =>
            {
                var data = ClipboardUtil.GetData<PropertyData>();
                if (data != null)
                {
                    AppendablePropertyModel.PasteChildrenProperty(data, SelectedChildren.OfType<PropertyGroupViewModel>().FirstOrDefault()?.InstanceId);
                }
            }, () => ClipboardUtil.GetData<PropertyData>()?.Type == CopyDataType.AppendablePropertyChildren);

            DeleteSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count > 0)
                {
                    AppendablePropertyModel.DeleteChildren(SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId).ToArray());
                }
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            DuplicateSelectedChildrenCommand = new DelegateCommand(() =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                AppendablePropertyModel.DuplicateChildrenProperty([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(p => p.InstanceId)]);
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            SavePropertyPresetCommand = new DelegateCommand(() =>
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
                    AppendablePropertyModel.SavePropertyPreset(filePath, [..SelectedChildren.OfType<PropertyGroupViewModel>().Select(p => p.InstanceId)]);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => SelectedChildren.Count > 0).ObservesProperty(() => SelectedChildren.Count);

            LoadPropertyPresetCommand = new DelegateCommand(() =>
            {
                var filePath = InternalPropertyViewModel.ShowPropertyPresetOpenDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                try
                {
                    AppendablePropertyModel.LoadPropertyPreset(filePath, SelectedChildren.OfType<PropertyGroupViewModel>().FirstOrDefault()?.InstanceId);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            DeleteCommand = new DelegateCommand<SelectItemType?>(_ => DeleteSelectedChildrenCommand.Execute(null));

            CutCommand = new DelegateCommand<SelectItemType?>(_ => CutSelectedChildrenCommand.Execute(null));

            CopyCommand = new DelegateCommand<SelectItemType?>(_ => CopySelectedChildrenCommand.Execute(null));

            PasteCommand = new DelegateCommand<SelectItemType?>(_ => PasteToSelectedChildrenCommand.Execute(null));

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => DuplicateSelectedChildrenCommand.Execute(null));

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

            AppendItemCommand = new DelegateCommand<AppendablePropertyItem>(i => AppendablePropertyModel.AddChild(i));

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

        private void Property_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            if (AppendablePropertyModel.ParentLayerIsLock)
            {
                DeSelect();
            }
            else if (e.OriginalSender is IInternalPropertyViewModel property && Children.Contains(property))
            {
                foreach (var child in Children)
                {
                    if (child is PropertyGroupViewModel childGroup && SelectedChildren.Contains(child))
                    {
                        childGroup.DeSelect();
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

        private void Property_IsEnableChangeRequest(object? sender, EventArgs e)
        {
            if (sender is not PropertyGroupViewModel child)
            {
                return;
            }

            if (SelectedChildren.Contains(child))
            {
                AppendablePropertyModel.ChangeIsEnable([.. SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId)], !child.IsEnable);
            }
            else
            {
                AppendablePropertyModel.ChangeIsEnable([child.InstanceId], !child.IsEnable);
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            switch (dropInfo.Data)
            {
                case PropertyGroupViewModel group when Children.Contains(group):
                case ItemDragData<IInternalPropertyViewModel> groups when groups.SelectedItems.OfType<PropertyGroupViewModel>().All(Children.Contains):
                    dropInfo.Effects = DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            switch (dropInfo.Data)
            {
                case PropertyGroupViewModel group when Children.Contains(group):
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Children.IndexOf(group) < newIndex)
                        {
                            newIndex--;
                        }
                        AppendablePropertyModel.MoveChild(group.InstanceId, newIndex);
                    }
                    break;
                case ItemDragData<IInternalPropertyViewModel> groups when groups.SelectedItems.OfType<PropertyGroupViewModel>().All(Children.Contains):
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Children.IndexOf(groups.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        var referencePropertyGroup = (PropertyGroupViewModel)groups.DragItem;
                        AppendablePropertyModel.MoveChildren(groups.SelectedItems.OfType<PropertyGroupViewModel>().Select(g => g.InstanceId).ToArray(), referencePropertyGroup.InstanceId, newIndex);
                    }
                    break;
            }
        }

        partial void WiringModel();

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
