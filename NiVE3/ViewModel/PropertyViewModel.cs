using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
        PropertyViewState ViewState { get; }

        event EventHandler<SelectItemEventArgs> SelectItemChanged;

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children { get; }

        void DeSelect();
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PropertyViewModel : BindableBase, IInternalPropertyViewModel
    {
        public PropertyViewState ViewState { get; }

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

        private ObservableCollection<KeyFrame> keyFrames = new ObservableCollection<KeyFrame>();
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

        private ObservableCollection<int> selectedKeyFrames = new ObservableCollection<int>();
        public ObservableCollection<int> SelectedKeyFrameIds
        {
            get { return selectedKeyFrames; }
            set
            {
                if (selectedKeyFrames != value)
                {
                    selectedKeyFrames.CollectionChanged -= SelectedKeyFrames_CollectionChanged;
                    value.CollectionChanged += SelectedKeyFrames_CollectionChanged;
                }
                SetProperty(ref selectedKeyFrames, value);
            }
        }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

        public PropertyBase Property { get; }

        public ICommand BeginEditCommand { get; }

        public ICommand EndEditCommand { get; }

        public ICommand AbortEditCommand { get; }

        public ICommand SwitchUseKeyFrameCommand { get; }

        public ICommand MoveTimeKeyFramesCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand ChangeKeyFramesInterpolationTypeCommand { get; }

        PropertyModel PropertyModel { get; }

        object? PrevValue { get; set; }

        bool IsEditing { get; set; }

        public PropertyViewModel(PropertyModel propertyModel)
        {
            PropertyModel = propertyModel;
            Property = propertyModel.Property;
            ViewState = propertyModel.CreateState(this);
            SelectedKeyFrameIds = new ObservableCollection<int>();

            BeginEditCommand = new RequerySuggestedCommand(() =>
            {
                PrevValue = CurrentTimeValue;
                IsEditing = true;
            }, () => !IsEditing);

            EndEditCommand = new RequerySuggestedCommand(() =>
            {
                PropertyModel.CommitProperty(CurrentTimeValue, PrevValue);
                IsEditing = false;
            }, () => IsEditing);

            AbortEditCommand = new RequerySuggestedCommand(() =>
            {
                CurrentTimeValue = PrevValue;
                IsEditing = false;
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

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.KeyFrame, true, this)));

            WiringModel();

            CurrentTimeValue = Value;
            HasKeyFrame = KeyFrames.Count > 0;

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

        object? CalculationValue()
        {
            if (KeyFrames.Count > 0)
            {
                return Property.PropertyType.Interpolate(KeyFrames, CurrentTime - SourceStartPoint);
            }
            else
            {
                return Value;
            }
        }

        partial void WiringModel();

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

        private void SelectedKeyFrames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.KeyFrame, false, this));
            }
        }
    }

    partial class PropertyGroupViewModel : BindableBase, IInternalPropertyViewModel
    {
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

        public PropertyBase Property { get; }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

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
            ViewState = propertyGroupModel.CreateState(this);
        }

        public void DeSelect()
        {
            foreach (var p in Children)
            {
                p.DeSelect();
            }
        }

        private void Property_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, e);
        }
    }
}
