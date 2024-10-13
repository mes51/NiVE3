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
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Shared.Extension;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.Util;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyViewModel : BindableBase, IInternalPropertyViewModel
    {
        public bool IsEnable => true;

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

        private bool useEditingValue;
        [NeedWire(nameof(PropertyModel))]
        public bool UseEditingValue
        {
            get { return useEditingValue; }
            set { SetProperty(ref useEditingValue, value); }
        }

        private bool useExpression;
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public bool UseExpression
        {
            get { return useExpression; }
            set { SetProperty(ref useExpression, value); }
        }

        private bool hasExpressionError;
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public bool HasExpressionError
        {
            get { return hasExpressionError; }
            set { SetProperty(ref hasExpressionError, value); }
        }

        private string expressionCode = "";
        [NeedWire(nameof(PropertyModel), IsOneWay = true)]
        public string ExpressionCode
        {
            get { return expressionCode; }
            set { SetProperty(ref expressionCode, value); }
        }

        private bool isEnableExpression;
        public bool IsEnableExpression
        {
            get { return isEnableExpression; }
            set { SetProperty(ref isEnableExpression, value); }
        }

        private object? currentTimeValue;
        public object? CurrentTimeValue
        {
            get { return currentTimeValue; }
            set { SetProperty(ref currentTimeValue, value); }
        }

        private object? currentTimeRawValue;
        public object? CurrentTimeRawValue
        {
            get { return currentTimeRawValue; }
            set { SetProperty(ref currentTimeRawValue, value); }
        }

        private bool hasKeyFrame;
        public bool HasKeyFrame
        {
            get { return hasKeyFrame; }
            set { SetProperty(ref hasKeyFrame, value); }
        }

        private ObservableCollection<int> selectedKeyFrameIds = [];
        public ObservableCollection<int> SelectedKeyFrameIds
        {
            get { return selectedKeyFrameIds; }
            set
            {
                if (selectedKeyFrameIds != value)
                {
                    selectedKeyFrameIds.CollectionChanged -= SelectedKeyFrameIds_CollectionChanged;
                    value.CollectionChanged += SelectedKeyFrameIds_CollectionChanged;
                }
                SetProperty(ref selectedKeyFrameIds, value);
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

        public ICommand AddKeyFrameToSelectedChildrenCommand { get; }

        public ICommand ResetSelectedChildrenCommand { get; }

        public ICommand CutSelectedChildrenCommand { get; }

        public ICommand CopySelectedChildrenCommand { get; }

        public ICommand PasteToSelectedChildrenCommand { get; }

        public ICommand DeleteSelectedChildrenCommand { get; }

        public ICommand DuplicateSelectedChildrenCommand { get; }

        public ICommand SelectKeyFrameCommand { get; }

        public ICommand ChangeKeyFramesInterpolationTypeCommand { get; }

        public ICommand PlaceKeyFrameCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        public DelegateCommand<SelectItemType?> AddKeyFrameCommand { get; }

        public DelegateCommand<SelectItemType?> ResetPropertyCommand { get; }

        private bool isEditing;
        bool IsEditing
        {
            get { return isEditing; }
            set { SetProperty(ref isEditing, value); }
        }

        PropertyModel PropertyModel { get; }

        object? PrevValue { get; set; }

        bool IsSelectingAll { get; set; }

        public PropertyViewModel(PropertyModel propertyModel)
        {
            PropertyModel = propertyModel;
            Property = propertyModel.Property;
            ViewState = propertyModel.CreateState(this);
            IsEnableExpression = propertyModel.IsEnableExpression;
            SelectedKeyFrameIds = [];

            BeginEditCommand = new DelegateCommand(() =>
            {
                PrevValue = CurrentTimeValue;
                IsEditing = true;
                UseEditingValue = true;
            }, () => !IsEditing).ObservesProperty(() => IsEditing);

            EndEditCommand = new DelegateCommand(() =>
            {
                PropertyModel.CommitProperty(CurrentTimeValue, PrevValue);
                IsEditing = false;
                UseEditingValue = false;
            }, () => IsEditing).ObservesProperty(() => IsEditing);

            AbortEditCommand = new DelegateCommand(() =>
            {
                CurrentTimeValue = PrevValue;
                IsEditing = false;
                UseEditingValue = false;
            }, () => IsEditing).ObservesProperty(() => IsEditing);

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

            PlaceKeyFrameCommand = new DelegateCommand(() =>
            {
                var time = CurrentTime;
                var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - time) < TimeCalc.TimeEpsilon || k.Time <= time);
                if (index > -1 && Math.Abs(KeyFrames[index].Time - time) < TimeCalc.TimeEpsilon)
                {
                    PropertyModel.DeleteKeyFrames([KeyFrames[index]]);
                }
                else
                {
                    PropertyModel.CreateKeyFrame(CurrentTimeValue);
                }
            }, () => KeyFrames.Count > 0).ObservesProperty(() => KeyFrames.Count);

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

            AddKeyFrameToSelectedChildrenCommand = new DelegateCommand(() =>
            {
                var time = CurrentTime;
                var index = KeyFrames.IndexOfLast(k => Math.Abs(k.Time - time) < TimeCalc.TimeEpsilon || k.Time <= time);
                if (index > -1 && Math.Abs(KeyFrames[index].Time - time) >= TimeCalc.TimeEpsilon)
                {
                    PropertyModel.CreateKeyFrame(CurrentTimeValue);
                }
            });

            ResetSelectedChildrenCommand = new DelegateCommand(() => PropertyModel.ResetProperty());

            CutSelectedChildrenCommand = new DelegateCommand(() =>
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
            }, () => SelectedKeyFrameIds.Count > 0).ObservesProperty(() => SelectedKeyFrameIds.Count);

            CopySelectedChildrenCommand = new DelegateCommand(() =>
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

            PasteToSelectedChildrenCommand = new RequerySuggestedCommand(() =>
            {
                var propertyData = ClipboardUtil.GetData<PropertyData>();
                if (propertyData != null)
                {
                    PropertyModel.PasteProperty(propertyData);
                }
            }, () => ClipboardUtil.GetData<PropertyData>()?.Type == CopyDataType.Property);

            DeleteSelectedChildrenCommand = new DelegateCommand(() =>
            {
                var keyFrames = SelectedKeyFrameIds.Select(id => KeyFrames.FirstOrDefault(k => k.Id == id)).NonNull().ToArray();
                if (keyFrames.Length > 0)
                {
                    PropertyModel.DeleteKeyFrames(keyFrames);
                }
                SelectedKeyFrameIds.Clear();
            }, () => SelectedKeyFrameIds.Count > 0).ObservesProperty(() => SelectedKeyFrameIds.Count);

            DuplicateSelectedChildrenCommand = new DelegateCommand(() => { });

            DeleteCommand = new DelegateCommand<SelectItemType?>(_ => DeleteSelectedChildrenCommand.Execute(null));

            CutCommand = new DelegateCommand<SelectItemType?>(_ => CutSelectedChildrenCommand.Execute(null));

            CopyCommand = new DelegateCommand<SelectItemType?>(_ => CopySelectedChildrenCommand.Execute(null));

            PasteCommand = new DelegateCommand<SelectItemType?>(_ => PasteToSelectedChildrenCommand.Execute(null));

            DuplicateCommand = new DelegateCommand<SelectItemType?>(_ => { });

            SelectAllCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                SelectAllKeyFrames();
            });

            AddKeyFrameCommand = new DelegateCommand<SelectItemType?>(_ => AddKeyFrameToSelectedChildrenCommand.Execute(null));

            ResetPropertyCommand = new DelegateCommand<SelectItemType?>(_ => ResetSelectedChildrenCommand.Execute(null));

            WiringModel();

            CurrentTimeRawValue = CalculationRawValue();
            CurrentTimeValue = CalculationValue();
            HasKeyFrame = KeyFrames.Count > 0;

            PropertyModel.ValueCommited += PropertyModel_ValueCommited;
            PropertyModel.PropertyChanged += PropertyModel_PropertyChanged;
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

        object? CalculationRawValue()
        {
            return PropertyModel.GetRawValue(CurrentTime - SourceStartPoint);
        }

        object? CalculationValue()
        {
            using var checker = CycleChecker.StartCheck();
            return PropertyModel.GetValue(CurrentTime - SourceStartPoint, CurrentTime);
        }

        partial void WiringModel();

        private void PropertyModel_ValueCommited(object? sender, EventArgs e)
        {
            PropertyValueUpdatePublisher.Publish(this, new PropertyValueCommitedEventArgs(this));
        }

        private void PropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyModel.RawValueUpdateKey && !IsEditing)
            {
                CurrentTimeRawValue = CalculationRawValue();
            }
        }

        private void PropertyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentTimeRawValue) when IsEditing:
                    PropertyModel.UpdateUncommitedRawValue(CurrentTimeRawValue);
                    CurrentTimeValue = CalculationValue();
                    break;
                case nameof(CurrentTimeRawValue):
                case nameof(CurrentTime):
                case nameof(SourceStartPoint):
                    CurrentTimeValue = CalculationValue();
                    break;
                case nameof(UseExpression):
                case nameof(HasExpressionError):
                    IsEnableExpression = PropertyModel.IsEnableExpression;
                    break;
            }
        }

        private void KeyFrames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasKeyFrame = KeyFrames.Count > 0;
            CurrentTimeRawValue = CalculationRawValue();
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
}
