using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Shared.Extension;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.View.Primitive;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    interface IInternalPropertyViewModel : IPropertyViewModel, IViewModelShortcutCommand
    {
        string Name { get; }

        PropertyViewState ViewState { get; }

        event EventHandler<SelectItemEventArgs> SelectItemChanged;

        event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited;

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children { get; }

        ICommand SelectItemCommand { get; }

        void DeSelect();
    }

    static class InternalPropertyViewModel
    {
        public static IInternalPropertyViewModel CreateViewModel(IPropertyModel model)
        {
            if (model is PropertyGroupModel pg)
            {
                return new PropertyGroupViewModel(pg);
            }
            else if (model is AppendablePropertyModel ap)
            {
                return new AppendablePropertyViewModel(ap);
            }
            else
            {
                return new PropertyViewModel((PropertyModel)model);
            }
        }
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyViewModel : BindableBase, IInternalPropertyViewModel
    {
        public PropertyViewState ViewState { get; }

        private string name = "";
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private double sourceStartPoint;
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double currentTime;
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private ObservableCollection<KeyFrame> keyFrames = [];
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public ObservableCollection<KeyFrame> KeyFrames
        {
            get { return keyFrames; }
            set
            {
                if (keyFrames != value)
                {
                    keyFrames.CollectionChanged -= KeyFrames_CollectionChanged;
                    value.CollectionChanged += KeyFrames_CollectionChanged;
                }
                SetProperty(ref keyFrames, value);
            }
        }

        public ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children => null;

        private object? _value;
        [NeedWire(nameof(PropertyModel))]
        public object? Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private bool useEditingValue;
        [NeedWire(nameof(PropertyModel))]
        public bool UseEditingValue
        {
            get { return useEditingValue; }
            set { SetProperty(ref useEditingValue, value); }
        }

        private object? currentTimeValue;
        public object? CurrentTimeValue
        {
            get { return currentTimeValue; }
            set { SetProperty(ref currentTimeValue, value); }
        }

        private bool hasKeyFrame;
        public bool HasKeyFrame
        {
            get { return hasKeyFrame; }
            set { SetProperty(ref hasKeyFrame, value); }
        }

        private ObservableCollection<int> selectedKeyFramesIds = [];
        public ObservableCollection<int> SelectedKeyFrameIds
        {
            get { return selectedKeyFramesIds; }
            set
            {
                if (selectedKeyFramesIds != value)
                {
                    selectedKeyFramesIds.CollectionChanged -= SelectedKeyFrameIds_CollectionChanged;
                    value.CollectionChanged += SelectedKeyFrameIds_CollectionChanged;
                }
                SetProperty(ref selectedKeyFramesIds, value);
            }
        }

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

        public PropertyBase Property { get; }

        public ICommand BeginEditCommand { get; }

        public ICommand EndEditCommand { get; }

        public ICommand AbortEditCommand { get; }

        public ICommand SwitchUseKeyFrameCommand { get; }

        public ICommand MoveTimeKeyFramesCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand SelectKeyFrameCommand { get; }

        public ICommand ChangeKeyFramesInterpolationTypeCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        PropertyModel PropertyModel { get; }

        object? PrevValue { get; set; }

        bool IsEditing { get; set; }

        bool IsSelectingAll { get; set; }

        public PropertyViewModel(PropertyModel propertyModel)
        {
            PropertyModel = propertyModel;
            Property = propertyModel.Property;
            ViewState = propertyModel.CreateState(this);
            SelectedKeyFrameIds = [];

            BeginEditCommand = new RequerySuggestedCommand(() =>
            {
                PrevValue = CurrentTimeValue;
                IsEditing = true;
                UseEditingValue = true;
            }, () => !IsEditing);

            EndEditCommand = new RequerySuggestedCommand(() =>
            {
                PropertyModel.CommitProperty(CurrentTimeValue, PrevValue);
                IsEditing = false;
                UseEditingValue = false;
            }, () => IsEditing);

            AbortEditCommand = new RequerySuggestedCommand(() =>
            {
                CurrentTimeValue = PrevValue;
                IsEditing = false;
                UseEditingValue = false;
            }, () => IsEditing);

            SwitchUseKeyFrameCommand = new DelegateCommand(() =>
            {
                if (KeyFrames.Count > 0)
                {
                    PropertyModel.ClearKeyFrame();
                }
                else
                {
                    PropertyModel.CreateKeyFrame(CurrentTimeValue);
                }
            });

            MoveTimeKeyFramesCommand = new DelegateCommand<Tuple<KeyFrame[], double[]>>(t =>
            {
                PropertyModel.MoveTimeKeyFrames(t.Item1, t.Item2);
            });

            ChangeKeyFramesInterpolationTypeCommand = new DelegateCommand<Tuple<KeyFrame[], InterpolationType>>(t =>
            {
                PropertyModel.ChangeKeyFramesInterpolationType(t.Item1, t.Item2);
            });

            SelectItemCommand = new DelegateCommand(() =>
            {
                SelectAllKeyFrames();
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Property, true, this));
            });

            SelectKeyFrameCommand = new DelegateCommand(() =>
            {
                if (SelectedKeyFrameIds.Count > 0)
                {
                    SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.KeyFrame, true, SelectedKeyFrameIds.ToArray(), this));
                }
            });

            DeleteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                var keyFrames = SelectedKeyFrameIds.Select(id => KeyFrames.FirstOrDefault(k => k.Id == id)).NonNull().ToArray();
                if (keyFrames.Length > 0)
                {
                    PropertyModel.DeleteKeyFrames(keyFrames);
                }
                SelectedKeyFrameIds.Clear();
            });

            CutCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (SelectedKeyFrameIds.Count > 0)
                {
                    var keyFrames = SelectedKeyFrameIds.Select(id => KeyFrames.FirstOrDefault(k => k.Id == id)).NonNull().ToArray();
                    if (keyFrames.Length > 0)
                    {
                        var copyData = PropertyModel.CutKeyFrames(keyFrames);
                        ClipboardUtil.SetData(copyData);
                    }
                    SelectedKeyFrameIds.Clear();
                }
                else
                {
                    var copyData = PropertyModel.CopyProperty();
                    ClipboardUtil.SetData(copyData);
                }
            });

            CopyCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (KeyFrames.Count > 0)
                {
                    var keyFrames = SelectedKeyFrameIds.Select(id => KeyFrames.FirstOrDefault(k => k.Id == id)).NonNull().ToArray();
                    if (keyFrames.Length > 0)
                    {
                        ClipboardUtil.SetData(PropertyModel.CopyKeyFrames(keyFrames));
                    }
                }
                else
                {
                    var copyData = PropertyModel.CopyProperty();
                    ClipboardUtil.SetData(copyData);
                }
            });

            PasteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                var propertyData = ClipboardUtil.GetData<PropertyData>();
                if (propertyData != null)
                {
                    PropertyModel.PasteProperty(propertyData);
                }
            });

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => { });

            WiringModel();

            CurrentTimeValue = Value;
            HasKeyFrame = KeyFrames.Count > 0;

            PropertyModel.ValueCommited += PropertyModel_ValueCommited;
            PropertyChanged += PropertyViewModel_PropertyChanged;
        }

        public PropertyControlBase CreateControl()
        {
            return PropertyModel.CreateControl(this);
        }

        public void DeSelect()
        {
            SelectedKeyFrameIds.Clear();
        }

        public void SelectAllKeyFrames()
        {
            IsSelectingAll = true;
            SelectedKeyFrameIds.Clear();
            foreach (var k in KeyFrames)
            {
                SelectedKeyFrameIds.Add(k.Id);
            }
            IsSelectingAll = false;
        }

        object? CalculationValue()
        {
            return PropertyModel.GetValue(CurrentTime - SourceStartPoint);
        }

        partial void WiringModel();

        private void PropertyModel_ValueCommited(object? sender, EventArgs e)
        {
            PropertyValueUpdatePublisher.Publish(this, new PropertyValueCommitedEventArgs(PropertyModel.Value, this));
        }

        private void PropertyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentTimeValue) when IsEditing:
                    Value = CurrentTimeValue;
                    break;
                case nameof(CurrentTime):
                case nameof(SourceStartPoint):
                    CurrentTimeValue = CalculationValue();
                    break;
                case nameof(Value):
                    CurrentTimeValue = Value;
                    break;
            }
        }

        private void KeyFrames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasKeyFrame = KeyFrames.Count > 0;
            CurrentTimeValue = CalculationValue();
        }

        private void SelectedKeyFrameIds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsSelectingAll && SelectedKeyFrameIds.Count > 0)
            {
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.KeyFrame, false, SelectedKeyFrameIds.ToArray(), this));
            }
        }
    }

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

        public object? CurrentTimeValue
        {
            get => null;
            set { }
        }

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

        public ICommand BeginEditCommand => throw new NotImplementedException();

        public ICommand EndEditCommand => throw new NotImplementedException();

        public ICommand AbortEditCommand => throw new NotImplementedException();

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand SelectItemCommand { get; }

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

        public PropertyGroupViewModel(PropertyGroupModel propertyGroupModel) : this(propertyGroupModel, false) { }

        public PropertyGroupViewModel(PropertyGroupModel propertyGroupModel, bool isRenameable)
        {
            SelectedChildren = [];
            PropertyGroupModel = propertyGroupModel;
            InstanceId = propertyGroupModel.InstanceId;
            Property = propertyGroupModel.Property;
            children = propertyGroupModel.Children.CreateViewCollection(m =>
            {
                var vm = InternalPropertyViewModel.CreateViewModel(m);
                vm.SelectItemChanged += Property_SelectItemChanged;
                vm.PropertyValueCommited += Property_PropertyValueCommited;
                return vm;
            });
            ViewState = propertyGroupModel.CreateState(this);
            IsRenameable = isRenameable;

            DeleteCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                var targetChildren = SelectedChildren.OfType<PropertyViewModel>().Select(c => c.Property.Id).ToArray();
                if (targetChildren.Length < 1)
                {
                    return;
                }

                PropertyGroupModel.DeleteChildrenKeyFrames(targetChildren);
            });

            CutCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                var targetChildren = SelectedChildren.OfType<PropertyViewModel>().Select(c => c.Property.Id).ToArray();
                if (targetChildren.Length < 1)
                {
                    return;
                }

                var data = PropertyGroupModel.CutChildrenKeyFrames(targetChildren);
                ClipboardUtil.SetData(data);
            });

            CopyCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var data = PropertyGroupModel.CopyChildrenProperty([..SelectedChildren.Select(c => c.Property.Id)]);
                ClipboardUtil.SetData(data);
            });

            PasteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                var propertyData = ClipboardUtil.GetData<PropertyData>();
                if (propertyData != null)
                {
                    PropertyGroupModel.PasteChildrenProperty(propertyData, [..SelectedChildren.Select(c => c.Property.Id)]);
                }
            });

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => { });

            BeginEditNameCommand = new RequerySuggestedCommand(() =>
            {
                PrevName = Name;
                IsNameEditing = true;
            }, () => IsRenameable && !IsNameEditing);

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit)
                {
                    PropertyGroupModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                IsNameEditing = false;
            }, _ => IsRenameable && IsNameEditing);

            SelectItemCommand = new DelegateCommand(() =>
            {
                DeSelect();
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Property, true, this));
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
            if (e.OriginalSender is IInternalPropertyViewModel property && Children.Contains(property))
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

    partial class AppendablePropertyViewModel : BindableBase, IInternalPropertyViewModel, IDropTarget, INameEditableParentViewModel
    {
        public string Name { get; }

        public PropertyViewState ViewState { get; }

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

        public object? CurrentTimeValue
        {
            get => null;
            set { }
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

        public ICommand BeginEditCommand => throw new NotImplementedException();

        public ICommand EndEditCommand => throw new NotImplementedException();

        public ICommand AbortEditCommand => throw new NotImplementedException();

        public ICommand SelectItemCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

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

        public AppendablePropertyViewModel(AppendablePropertyModel appendablePropertyModel)
        {
            SelectedChildren = [];
            AppendablePropertyModel = appendablePropertyModel;
            Property = appendablePropertyModel.Property;
            children = appendablePropertyModel.Children.CreateViewCollection(m =>
            {
                var vm = new PropertyGroupViewModel((PropertyGroupModel)m, true);
                vm.SelectItemChanged += Property_SelectItemChanged;
                vm.PropertyValueCommited += Property_PropertyValueCommited;
                return (IInternalPropertyViewModel)vm;
            });
            Name = appendablePropertyModel.Name;
            ViewState = appendablePropertyModel.CreateState(this);

            SelectItemCommand = new DelegateCommand(() =>
            {
                DeSelect();
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Property, true, this));
            });

            DeleteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (SelectedChildren.Count > 0)
                {
                    AppendablePropertyModel.DeleteChildren(SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId).ToArray());
                }
            });

            CutCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (SelectedChildren.Count > 0)
                {
                    AppendablePropertyModel.DeleteChildren(SelectedChildren.OfType<PropertyGroupViewModel>().Select(c => c.InstanceId).ToArray());
                }
            });

            CopyCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (SelectedChildren.Count < 1)
                {
                    return;
                }

                var data = AppendablePropertyModel.CopyChildrenProperty([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(p => p.InstanceId)]);
                ClipboardUtil.SetData(data);
            });

            PasteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                var data = ClipboardUtil.GetData<PropertyData>();
                if (data != null)
                {
                    AppendablePropertyModel.PasteChildrenProperty(data);
                }
            });

            DuplicateCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                AppendablePropertyModel.DuplicateChildrenProperty([..SelectedChildren.OfType<PropertyGroupViewModel>().Select(p => p.InstanceId)]);
            });

            AppendItemCommand = new DelegateCommand<AppendablePropertyItem>(i => AppendablePropertyModel.AddChild(i));
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
            if (e.OriginalSender is IInternalPropertyViewModel property && Children.Contains(property))
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
