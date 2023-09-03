using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerViewModel : BindableBase
    {
        private Guid layerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid LayerId
        {
            get { return layerId; }
            set { SetProperty(ref layerId, value); }
        }

        private string name = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private double duration;
        [NeedWire(nameof(LayerModel))]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double sourceStartPoint;
        [NeedWire(nameof(LayerModel))]
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double inPoint;
        [NeedWire(nameof(LayerModel))]
        public double InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private double outPoint;
        [NeedWire(nameof(LayerModel))]
        public double OutPoint
        {
            get { return outPoint; }
            set { SetProperty(ref outPoint, value); }
        }

        private bool isEnableTimeRemap;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableTimeRemap
        {
            get { return isEnableTimeRemap; }
            set { SetProperty(ref isEnableTimeRemap, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private Color tagColor;
        [NeedWire(nameof(LayerModel))]
        public Color TagColor
        {
            get { return tagColor; }
            set { SetProperty(ref tagColor, value); }
        }

        private bool isEnableVideo;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableVideo
        {
            get { return isEnableVideo; }
            set { SetProperty(ref isEnableVideo, value); }
        }

        private bool isEnableAudio;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableAudio
        {
            get { return isEnableAudio; }
            set { SetProperty(ref isEnableAudio, value); }
        }

        private bool isEnableSolo;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableSolo
        {
            get { return isEnableSolo; }
            set { SetProperty(ref isEnableSolo, value); }
        }

        private bool isLock;
        [NeedWire(nameof(LayerModel))]
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private bool isEnableShy;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableCollapse;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableCollapse
        {
            get { return isEnableCollapse; }
            set { SetProperty(ref isEnableCollapse, value); }
        }

        private bool isEnableEffect;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableEffect
        {
            get { return isEnableEffect; }
            set { SetProperty(ref isEnableEffect, value); }
        }

        private bool isEnableFrameBlend;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private bool isEnableAdjustmentLayer;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableAdjustmentLayer
        {
            get { return isEnableAdjustmentLayer; }
            set { SetProperty(ref isEnableAdjustmentLayer, value); }
        }

        private bool isEnable3D;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private ImageInterpolationQuality interpolationQuality;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public ImageInterpolationQuality InterpolationQuality
        {
            get { return interpolationQuality; }
            set { SetProperty(ref interpolationQuality, value); }
        }

        private bool hasEffect;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool HasEffect
        {
            get { return hasEffect; }
            set { SetProperty(ref hasEffect, value); }
        }

        private double layerNumberColumnWudth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnWidth), IsOneWay = true)]
        public double LayerNumberColumnWidth
        {
            get { return layerNumberColumnWudth; }
            set { SetProperty(ref layerNumberColumnWudth, value); }
        }

        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNameColumnWidth), IsOneWay = true)]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerCommentColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnWidth), IsOneWay = true)]
        public double LayerCommentColumnWidth
        {
            get { return layerCommentColumnWidth; }
            set { SetProperty(ref layerCommentColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnWidth), IsOneWay = true)]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private double modeColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnWidth), IsOneWay = true)]
        public double ModeColumnWidth
        {
            get { return modeColumnWidth; }
            set { SetProperty(ref modeColumnWidth, value); }
        }

        private double parentLayerColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnWidth), IsOneWay = true)]
        public double ParentLayerColumnWidth
        {
            get { return parentLayerColumnWidth; }
            set { SetProperty(ref parentLayerColumnWidth, value); }
        }

        private bool isAVSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineAVSwitchColumnVisible), IsOneWay = true)]
        public bool IsAVSwitchColumnVisible
        {
            get { return isAVSwitchColumnVisible; }
            set { SetProperty(ref isAVSwitchColumnVisible, value); }
        }

        private bool isTagColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTagColumnVisible), IsOneWay = true)]
        public bool IsTagColumnVisible
        {
            get { return isTagColumnVisible; }
            set { SetProperty(ref isTagColumnVisible, value); }
        }

        private bool isLayerNumberColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnVisible), IsOneWay = true)]
        public bool IsLayerNumberColumnVisible
        {
            get { return isLayerNumberColumnVisible; }
            set { SetProperty(ref isLayerNumberColumnVisible, value); }
        }

        private bool isLayerCommentColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnVisible), IsOneWay = true)]
        public bool IsLayerCommentColumnVisible
        {
            get { return isLayerCommentColumnVisible; }
            set { SetProperty(ref isLayerCommentColumnVisible, value); }
        }

        private bool isLayerSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnVisible), IsOneWay = true)]
        public bool IsLayerSwitchColumnVisible
        {
            get { return isLayerSwitchColumnVisible; }
            set { SetProperty(ref isLayerSwitchColumnVisible, value); }
        }

        private bool isModeColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnVisible), IsOneWay = true)]
        public bool IsModeColumnVisible
        {
            get { return isModeColumnVisible; }
            set { SetProperty(ref isModeColumnVisible, value); }
        }

        private bool isParentLayerColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnVisible), IsOneWay = true)]
        public bool IsParentLayerColumnVisible
        {
            get { return isParentLayerColumnVisible; }
            set { SetProperty(ref isParentLayerColumnVisible, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        private EditingLayerParameter editingParameter;
        public EditingLayerParameter EditingParameter
        {
            get { return editingParameter; }
            set { SetProperty(ref editingParameter, value); }
        }

        public bool IsComposition { get; }

        public ICommand BeginEditDurationCommand { get; }

        public ICommand CommitEditDurationCommand { get; }

        public ICommand ChangeLayerSwitchCommand { get; }

        public ICommand ChangeInterpolationQualityCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        WeakEventPublisher<LayerSwitchEventArgs> LayerSwitchChangeRequestPublisher { get; } = new WeakEventPublisher<LayerSwitchEventArgs>();
        public event EventHandler<LayerSwitchEventArgs> LayerSwitchChangeRequest
        {
            add { LayerSwitchChangeRequestPublisher.Subscribe(value); }
            remove { LayerSwitchChangeRequestPublisher.Unsubscribe(value); }
        }

        LayerModel LayerModel { get; }

        ViewStateModel ViewState { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        public LayerViewModel(LayerModel layerModel, ViewStateModel viewState)
        {
            LayerModel = layerModel;
            ViewState = viewState;

            WiringModel();

            IsComposition = layerModel.IsComposition;

            BeginEditDurationCommand = new RequerySuggestedCommand(() =>
            {
                LayerModel.BeginEditDuration();
                EditingParameter = EditingLayerParameter.Duration;
            }, () => EditingParameter == EditingLayerParameter.None);

            CommitEditDurationCommand = new RequerySuggestedCommand(() =>
            {
                LayerModel.CommitEditDuration();
                EditingParameter = EditingLayerParameter.None;
            }, () => EditingParameter == EditingLayerParameter.Duration);

            ChangeLayerSwitchCommand = new DelegateCommand<string>(name =>
            {
                var newValue = !(name switch {
                    nameof(IsEnableVideo) => IsEnableVideo,
                    nameof(IsEnableAudio) => IsEnableAudio,
                    nameof(IsEnableSolo) => IsEnableSolo,
                    nameof(IsLock) => IsLock,
                    nameof(IsEnableShy) => IsEnableShy,
                    nameof(IsEnableCollapse) => IsEnableCollapse,
                    nameof(IsEnableEffect) => IsEnableEffect,
                    nameof(IsEnableFrameBlend) => IsEnableFrameBlend,
                    nameof(IsEnableMotionBlur) => IsEnableMotionBlur,
                    nameof(IsEnableAdjustmentLayer) => IsEnableAdjustmentLayer,
                    nameof(IsEnable3D) => IsEnable3D,
                    _ => false
                });
                LayerSwitchChangeRequestPublisher.Publish(this, new LayerSwitchEventArgs(name, newValue));
            });

            ChangeInterpolationQualityCommand = new DelegateCommand(() =>
            {
                var values = Enum.GetValues<ImageInterpolationQuality>();
                var newValue = values[(Array.IndexOf(values, InterpolationQuality) + 1) % values.Length];
                LayerSwitchChangeRequestPublisher.Publish(this, new LayerSwitchEventArgs(nameof(InterpolationQuality), newValue));
            });

            BeginEditNameCommand = new RequerySuggestedCommand(() =>
            {
                PrevName = Name;
                EditingParameter = EditingLayerParameter.Name;
            }, () => EditingParameter == EditingLayerParameter.None);

            BeginEditCommentCommand = new RequerySuggestedCommand(() =>
            {
                PrevComment = Comment;
                EditingParameter = EditingLayerParameter.Comment;
            }, () => EditingParameter == EditingLayerParameter.None);

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit && !string.IsNullOrEmpty(Name))
                {
                    LayerModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                EditingParameter = EditingLayerParameter.None;
            }, _ => EditingParameter == EditingLayerParameter.Name);

            EndEditCommentCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit)
                {
                    LayerModel.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                EditingParameter = EditingLayerParameter.None;
            }, _ => EditingParameter == EditingLayerParameter.Comment);
        }

        partial void WiringModel();
    }

    enum EditingLayerParameter
    {
        None,
        Name,
        Comment,
        Duration
    }
}
