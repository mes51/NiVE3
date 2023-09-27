using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Input;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using Prism.Mvvm;
using NiVE3.Plugin.Resource;
using NiVE3.View.Resource;
using NiVE3.Plugin.Struct;
using NiVE3.Property;
using System.ComponentModel;
using NiVE3.Shared.Extension;
using NiVE3.Extension;

namespace NiVE3.Model
{
    partial class LayerModel : BindableBase, IDisposable, ILayerObject
    {
        const string TransformGroupId = nameof(TransformGroupId);

        const string TransformAnchorPointId= nameof(TransformAnchorPointId);

        const string TransformTranslateId = nameof(TransformTranslateId);

        const string TransformDirectionId = nameof(TransformDirectionId);

        const string TransformXAngleId = nameof(TransformXAngleId);

        const string TransformYAngleId = nameof(TransformYAngleId);

        const string TransformZAngleId = nameof(TransformZAngleId);

        const string TransformScaleId = nameof(TransformScaleId);

        const string TransformPropertyOpacityId = nameof(TransformPropertyOpacityId);

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double sourceStartPoint;
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double inPoint;
        public double InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private double outPoint;
        public double OutPoint
        {
            get { return outPoint; }
            set { SetProperty(ref outPoint, value); }
        }

        private bool isEnableTimeRemap;
        public bool IsEnableTimeRemap
        {
            get { return isEnableTimeRemap; }
            set { SetProperty(ref isEnableTimeRemap, value); }
        }

        private SourceType sourceType;
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private Color tagColor = Colors.Red;
        public Color TagColor
        {
            get { return tagColor; }
            set { SetProperty(ref tagColor, value); }
        }

        private bool isEnableVideo;
        public bool IsEnableVideo
        {
            get { return isEnableVideo; }
            set { SetProperty(ref isEnableVideo, value); }
        }

        private bool isEnableAudio;
        public bool IsEnableAudio
        {
            get { return isEnableAudio; }
            set { SetProperty(ref isEnableAudio, value); }
        }

        private bool isEnableSolo;
        public bool IsEnableSolo
        {
            get { return isEnableSolo; }
            set { SetProperty(ref isEnableSolo, value); }
        }

        private bool isLock;
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private bool isEnableShy;
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableCollapse;
        public bool IsEnableCollapse
        {
            get { return isEnableCollapse; }
            set { SetProperty(ref isEnableCollapse, value); }
        }

        private bool isEnableEffect = true;
        public bool IsEnableEffect
        {
            get { return isEnableEffect; }
            set { SetProperty(ref isEnableEffect, value); }
        }

        private bool isEnableFrameBlend;
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private bool isEnableAdjustmentLayer;
        public bool IsEnableAdjustmentLayer
        {
            get { return isEnableAdjustmentLayer; }
            set { SetProperty(ref isEnableAdjustmentLayer, value); }
        }

        private bool isEnable3D;
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private ImageInterpolationQuality interpolationQuality = ImageInterpolationQuality.Level2;
        public ImageInterpolationQuality InterpolationQuality
        {
            get { return interpolationQuality; }
            set { SetProperty(ref interpolationQuality, value); }
        }

        private bool hasEffect;
        public bool HasEffect
        {
            get { return hasEffect; }
            set { SetProperty(ref hasEffect, value); }
        }

        private BlendMode blendMode = BlendMode.Normal;
        public BlendMode BlendMode
        {
            get { return blendMode; }
            set { SetProperty(ref blendMode, value); }
        }

        private Guid? trackMatteLayerId;
        public Guid? TrackMatteLayerId
        {
            get { return trackMatteLayerId; }
            set { SetProperty(ref trackMatteLayerId, value); }
        }

        private TrackMatteMode trackMatteMode = TrackMatteMode.Alpha;
        public TrackMatteMode TrackMatteMode
        {
            get { return trackMatteMode; }
            set { SetProperty(ref trackMatteMode, value); }
        }

        private Guid? parentLayerId;
        public Guid? ParentLayerId
        {
            get { return parentLayerId; }
            set { SetProperty(ref parentLayerId, value); }
        }

        public Guid LayerId { get; }

        public string SourceName => FootageModel.Name;

        public bool IsComposition => FootageModel.InputModel.Input is CompositionInput;

        private ObservableCollection<EffectModel> effects = new ObservableCollection<EffectModel>();
        public ObservableCollection<EffectModel> Effects
        {
            get { return effects; }
            set
            {
                if (effects!= value)
                {
                    effects.CollectionChanged -= Effects_CollectionChanged;
                    value.CollectionChanged += Effects_CollectionChanged;
                }
                SetProperty(ref effects, value);
            }
        }

        public FootageModel FootageModel { get; }

        public PropertyGroupModel TransformProperties { get; }

        EffectListModel EffectListModel { get; }

        CompositionModel CompositionModel { get; }

        HistoryModel HistoryModel { get; set; }

        double PrevInPoint { get; set; }

        double PrevOutPoint { get; set; }

        double PrevSourceStartPoint { get; set; }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel) : this(compositionModel, footageModel, effectListModel, historyModel, null) { }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, Guid? layerId)
        {
            Effects = new ObservableCollection<EffectModel>();
            FootageModel = footageModel;
            EffectListModel = effectListModel;
            CompositionModel = compositionModel;
            HistoryModel = historyModel;
            Name = footageModel.Name;
            Duration = footageModel.Duration;
            OutPoint = footageModel.Duration;
            SourceType = footageModel.InputType;
            LayerId = layerId ?? Guid.NewGuid();

            IsEnableVideo = SourceType.HasFlag(SourceType.Video) || SourceType.HasFlag(SourceType.Image);
            IsEnableAudio = SourceType.HasFlag(SourceType.Audio);

            TransformProperties = new PropertyGroupModel(new PropertyGroup(TransformGroupId, CreateLanguageResourceKey(LanguageResourceDictionary.Layer_Transform), new PropertyBase[]
            {
                new Vector2DOr3DProperty(TransformAnchorPointId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_AnchorPoint), new Vector3d(footageModel.Width * 0.5, footageModel.Height * 0.5, 0.0), true, 2),
                new Vector2DOr3DProperty(TransformTranslateId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_Translate), new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), true, 2),
                new Direction3DProperty(TransformDirectionId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_Direction), new Vector3d(), true, 2),
                new Angle3DElementProperty(TransformXAngleId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_XAngle3D), 0.0, true, 2),
                new Angle3DElementProperty(TransformYAngleId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_YAngle3D), 0.0, true, 2),
                new ZAngleProperty(TransformZAngleId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_ZAngle2D), CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_ZAngle3D), 0.0, true, 2),
                new Scale2DOr3DProperty(TransformScaleId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_Scale), new Vector3d(100.0, 100.0, 100.0), true, 2),
                new DoubleProperty(TransformPropertyOpacityId, CreateLanguageResourceKey(LanguageResourceDictionary.TransformProperty_Opacity), 100.0, 0.0, 100.0, true, 1.0, 2)
            }), compositionModel, this, historyModel);
        }

        public void BeginEditDuration()
        {
            PrevInPoint = InPoint;
            PrevOutPoint = OutPoint;
            PrevSourceStartPoint = SourceStartPoint;
        }

        public void CommitEditDuration()
        {
            if (PrevInPoint != inPoint || PrevOutPoint != OutPoint || PrevSourceStartPoint != SourceStartPoint)
            {
                HistoryModel.Add(new EditDurationHistoryCommand(this, PrevInPoint, PrevOutPoint, PrevSourceStartPoint, InPoint, OutPoint, SourceStartPoint));
            }
        }

        public void ChangeName(string name)
        {
            if (Name != name)
            {
                var prevName = Name;
                Name = name;
                HistoryModel.Add(new ChangeNameHistoryCommand(this, prevName, name));
            }
        }

        public void ChangeComment(string comment)
        {
            if (Comment != comment)
            {
                var prevComment = Comment;
                Comment = comment;
                HistoryModel.Add(new ChangeCommentHistoryCommand(this, prevComment, comment));
            }
        }

        public void InsertEffect(Guid[] effectIds, int index)
        {
            var effectModels = effectIds.Select(id => EffectListModel.CreateEffect(id, CompositionModel, this, HistoryModel)).OfType<EffectModel>().ToArray();

            var i = index;
            foreach (var e in effectModels)
            {
                Effects.Insert(i, e);
                i++;
            }

            HistoryModel.Add(new InsertEffectsHistoryCommand(this, effectModels, index));
        }

        public void MoveEffect(Guid effectId, int newIndex)
        {
            MoveEffects(new Guid[] { effectId }, effectId, newIndex);
        }

        public void MoveEffects(Guid[] effectIds, Guid referenceEffectId, int newIndex)
        {
            if (Effects.Count == effectIds.Length)
            {
                return;
            }

            var effects = Effects.Where(l => effectIds.Contains(l.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var prevIndices = effects.Select(l => Effects.IndexOf(l)).ToArray();
            var startIndex = newIndex - effects.IndexOf(l => l.EffectId == referenceEffectId);
            var newOrderedEffects = new List<EffectModel>(Effects.Count);
            newOrderedEffects.AddRange(Effects.Except(effects).Take(startIndex));
            newOrderedEffects.AddRange(effects);
            newOrderedEffects.AddRange(Effects.Except(newOrderedEffects.ToArray()));

            Effects.SortBy(l => newOrderedEffects.IndexOf(l));

            if (!prevIndices.SequenceEqual(effects.Select(l => Effects.IndexOf(l))))
            {
                HistoryModel.Add(new MoveEffectsHistoryCommand(this, effects, prevIndices, newOrderedEffects.ToArray()));
            }
        }

        public void ChangeEffectEnable(Guid[] effectIds, bool isEnable)
        {
            var effects = Effects.Where(e => effectIds.Contains(e.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldValues = effects.Select(e => e.IsEnable).ToArray();

            foreach (var e in effects)
            {
                e.IsEnable = isEnable;
            }

            HistoryModel.Add(new ChangeEffectEnableHistoryCommand(effects, oldValues, isEnable));
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasEffect = Effects.Count > 0;
        }

        public void Dispose()
        {
        }

        static LanguageResourceKey CreateLanguageResourceKey(string key)
        {
            return new LanguageResourceKey(typeof(LanguageResourceDictionary), key);
        }
    }
}
