using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Exceptions;
using NiVE3.Numerics;
using NiVE3.Plugin.ValueObject;
using NiVE3.Text;

namespace NiVE3.Model
{
    class CompositionEventArgs : EventArgs
    {
        public CompositionModel Composition { get; }

        public CompositionEventArgs(CompositionModel composition)
        {
            Composition = composition;
        }
    }

    class ShowLoadSettingEventArgs : EventArgs
    {
        public ShowLoadSettingEventArgs(FrameworkElement view)
        {
            View = view;
        }

        public FrameworkElement View { get; }

        public bool IsOK { get; set; }
    }

    class FootageModelEventArgs : EventArgs
    {
        public FootageModelEventArgs(FootageModel footage)
        {
            Footage = footage;
        }

        public FootageModel Footage { get; }
    }

    class FootageEventArgs : EventArgs
    {
        public FootageEventArgs(IFootageModel footage) : this([footage]) { }

        public FootageEventArgs(IFootageModel[] footages)
        {
            Footages = footages;
        }

        public IFootageModel[] Footages { get; }
    }

    class SelectLayerEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Vector2d ScreenPosition { get; }

        public Vector2d PreviewImageScale { get; }

        public Time CurrentTime { get; }

        public SelectLayerEventArgs(Guid compositionId, Vector2d screenPosition, Vector2d previewImageScale, Time currentTime)
        {
            CompositionId = compositionId;
            ScreenPosition = screenPosition;
            PreviewImageScale = previewImageScale;
            CurrentTime = currentTime;
        }
    }

    class BeginUseToolEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Vector2d StartScreenPosition { get; }

        public Vector2d PreviewImageScale { get; }

        public PropertyType Type { get; }

        public Time CurrentTime { get; }

        public BeginUseToolEventArgs(Guid compositionId, Vector2d startScreenPosition, Vector2d previewImageScale, PropertyType type, Time currentTime)
        {
            CompositionId = compositionId;
            StartScreenPosition = startScreenPosition;
            PreviewImageScale = previewImageScale;
            Type = type;
            CurrentTime = currentTime;
        }

        [Flags]
        public enum PropertyType : int
        {
            None = 0,
            LayerProperty = 0b01000000,
            CameraProperty = 0b10000000,
            Transform = LayerProperty | 0x01,
            RotateAll = LayerProperty | 0x0E,
            RotateX = LayerProperty | 0x02,
            RotateY = LayerProperty | 0x04,
            RotateZ = LayerProperty | 0x08,
            Scale = LayerProperty | 0x10,
            CameraOrbit = CameraProperty | 0x01,
            CameraPan = CameraProperty | 0x02,
            CameraDolly = CameraProperty | 0x4
        }
    }

    class MoveLayersByToolEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Vector2d NextScreenPos { get; }

        public Vector2d PreviewImageScale { get; }

        public bool IsCommit { get; }

        public Time CurrentTime { get; }

        public MoveLayersByToolEventArgs(Guid compositionId, Vector2d nextScreenPos, Vector2d previewImageScale, bool isCommit, Time currentTime)
        {
            CompositionId = compositionId;
            NextScreenPos = nextScreenPos;
            PreviewImageScale = previewImageScale;
            IsCommit = isCommit;
            CurrentTime = currentTime;
        }
    }

    class AbortUseToolEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public AbortUseToolEventArgs(Guid compositionId)
        {
            CompositionId = compositionId;
        }
    }

    class AddEffectEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Guid? TargetLayerId { get; }

        public Guid[] EffectPluginIds { get; }

        public AddEffectEventArgs(Guid compositionId, Guid? targetLayerId, Guid[] effectPluginIds)
        {
            CompositionId = compositionId;
            TargetLayerId = targetLayerId;
            EffectPluginIds = effectPluginIds;
        }
    }

    class BeginEditDurationEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Guid LayerId { get; }

        public DurationType Type { get; }

        public BeginEditDurationEventArgs(Guid compositionId, Guid layerId, DurationType type)
        {
            CompositionId = compositionId;
            LayerId = layerId;
            Type = type;
        }

        public enum DurationType
        {
            None,
            InPoint,
            OutPoint,
            SourceStartPoint,
            Slip
        }
    }

    class UpdateDurationEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Time InPointDiff { get; }

        public Time OutPointDiff { get; }

        public Time SourceStartPointDiff { get; }

        public bool IsCommit { get; }

        public UpdateDurationEventArgs(Guid compositionId, Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff, bool isCommit)
        {
            CompositionId = compositionId;
            InPointDiff = inPointDiff;
            OutPointDiff = outPointDiff;
            SourceStartPointDiff = sourceStartPointDiff;
            IsCommit = isCommit;
        }
    }

    class AbortEditDurationEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public AbortEditDurationEventArgs(Guid compositionId)
        {
            CompositionId = compositionId;
        }
    }

    class NeedHistoryChangeEventArgs : EventArgs
    {
        public bool NeedHistoryChange { get; }

        public NeedHistoryChangeEventArgs(bool needHistoryChange)
        {
            NeedHistoryChange = needHistoryChange;
        }
    }

    class ShowFootagePreviewEventArgs : EventArgs
    {
        public Guid FootageId { get; }

        public ShowFootagePreviewEventArgs(Guid footageId)
        {
            FootageId = footageId;
        }
    }

    class TextStyleChangeEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Guid? TargetLayerId { get; }

        public object? TargetLayerPrevValue { get; }

        public TextStyleChangeEventArgs(Guid compositionId, Guid? targetLayerId, object? targetLayerPrevValue)
        {
            CompositionId = compositionId;
            TargetLayerId = targetLayerId;
            TargetLayerPrevValue = targetLayerPrevValue;
        }
    }

    class RenderPreviewInteractionEventArgs : EventArgs
    {
        public Guid CompositionId { get; }

        public Time CurrentTime { get; }

        public DrawingContext DrawingContext { get; }

        public Vector2d PreviewImagePosition { get; }

        public Vector2d PreviewImageScale { get; }

        public RenderPreviewInteractionEventArgs(Guid compositionId, Time currentTime, DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale)
        {
            CompositionId = compositionId;
            CurrentTime = currentTime;
            DrawingContext = drawingContext;
            PreviewImagePosition = previewImagePosition;
            PreviewImageScale = previewImageScale;
        }
    }

    class RaiseGPUExceptionEventArgs : EventArgs
    {
        public GPUException Exception { get; }

        public RaiseGPUExceptionEventArgs(GPUException exception)
        {
            Exception = exception;
        }
    }
}
