using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Mvvm;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.ValueObject;
using NiVE3.Text;
using NiVE3.ViewModel;
using Prism.Mvvm;

namespace NiVE3.Model.UI
{
    class EventHubModel : BindableBase
    {
        WeakEventPublisher<SelectLayerEventArgs> SelectLayerRequestPublisher { get; } = new WeakEventPublisher<SelectLayerEventArgs>();
        public event EventHandler<SelectLayerEventArgs> SelectLayerRequest
        {
            add { SelectLayerRequestPublisher.Subscribe(value); }
            remove { SelectLayerRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<BeginUseToolEventArgs> BeginUseToolRequestPublisher { get; } = new WeakEventPublisher<BeginUseToolEventArgs>();
        public event EventHandler<BeginUseToolEventArgs> BeginUseToolRequest
        {
            add { BeginUseToolRequestPublisher.Subscribe(value); }
            remove { BeginUseToolRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<MoveLayersByToolEventArgs> MoveLayersByToolRequestPublisher { get; } = new WeakEventPublisher<MoveLayersByToolEventArgs>();
        public event EventHandler<MoveLayersByToolEventArgs> MoveLayersByToolRequest
        {
            add { MoveLayersByToolRequestPublisher.Subscribe(value); }
            remove { MoveLayersByToolRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<AbortUseToolEventArgs> AbortUseToolRequestPublisher { get; } = new WeakEventPublisher<AbortUseToolEventArgs>();
        public event EventHandler<AbortUseToolEventArgs> AbortUseToolRequest
        {
            add { AbortUseToolRequestPublisher.Subscribe(value); }
            remove { AbortUseToolRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<AddEffectEventArgs> AddEffectToSelectedLayersPublisher { get; } = new WeakEventPublisher<AddEffectEventArgs>();
        public event EventHandler<AddEffectEventArgs> AddEffectToSelectedLayers
        {
            add { AddEffectToSelectedLayersPublisher.Subscribe(value); }
            remove { AddEffectToSelectedLayersPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<BeginEditDurationEventArgs> BeginEditDurationRequestPublisher { get; } = new WeakEventPublisher<BeginEditDurationEventArgs>();
        public event EventHandler<BeginEditDurationEventArgs> BeginEditDurationRequest
        {
            add { BeginEditDurationRequestPublisher.Subscribe(value); }
            remove { BeginEditDurationRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<UpdateDurationEventArgs> UpdateDurationRequestPublisher { get; } = new WeakEventPublisher<UpdateDurationEventArgs>();
        public event EventHandler<UpdateDurationEventArgs> UpdateDurationRequest
        {
            add { UpdateDurationRequestPublisher.Subscribe(value); }
            remove { UpdateDurationRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<AbortEditDurationEventArgs> AbortEditDurationRequestPublisher { get; } = new WeakEventPublisher<AbortEditDurationEventArgs>();
        public event EventHandler<AbortEditDurationEventArgs> AbortEditDurationRequest
        {
            add { AbortEditDurationRequestPublisher.Subscribe(value); }
            remove { AbortEditDurationRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<ShowFootagePreviewEventArgs> ShowFootagePreviewRequestPublisher { get; } = new WeakEventPublisher<ShowFootagePreviewEventArgs>();
        public event EventHandler<ShowFootagePreviewEventArgs> ShowFootagePreviewRequest
        {
            add { ShowFootagePreviewRequestPublisher.Subscribe(value); }
            remove { ShowFootagePreviewRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> PlayOrStopRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> PlayOrStopRequest
        {
            add { PlayOrStopRequestPublisher.Subscribe(value); }
            remove { PlayOrStopRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<TextStyleChangeEventArgs> TextStyleChangeRequestPublisher { get; } = new WeakEventPublisher<TextStyleChangeEventArgs>();
        public event EventHandler<TextStyleChangeEventArgs> TextStyleChangeRequest
        {
            add { TextStyleChangeRequestPublisher.Subscribe(value); }
            remove { TextStyleChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<RenderPreviewInteractionEventArgs> RenderPreviewInteractionRequestPublisher { get; } = new WeakEventPublisher<RenderPreviewInteractionEventArgs>();
        public event EventHandler<RenderPreviewInteractionEventArgs> RenderPreviewInteractionRequest
        {
            add { RenderPreviewInteractionRequestPublisher.Subscribe(value); }
            remove { RenderPreviewInteractionRequestPublisher.Unsubscribe(value); }
        }

        public SelectPreviewResult NotifySelectLayer(Guid compositionId, Vector2d screenPos, Vector2d previewImageScale, Time currentTime)
        {
            var eventArgs = new SelectLayerEventArgs(compositionId, screenPos, previewImageScale, currentTime);
            SelectLayerRequestPublisher.Publish(this, eventArgs);
            return eventArgs.Selected;
        }

        public void NotifyBeginUseTool(Guid compositionId, Vector2d startScreenPos, Vector2d previewImageScale, BeginUseToolEventArgs.PropertyType propertyType, Time currentTime)
        {
            BeginUseToolRequestPublisher.Publish(this, new BeginUseToolEventArgs(compositionId, startScreenPos, previewImageScale, propertyType, currentTime));
        }

        public void NotifyMoveLayersByTool(Guid compositionId, Vector2d nextScreenPos, Vector2d previewImageScale, bool isCommit, Time currentTime)
        {
            MoveLayersByToolRequestPublisher.Publish(this, new MoveLayersByToolEventArgs(compositionId, nextScreenPos, previewImageScale, isCommit, currentTime));
        }

        public void NotifyAbortUseTool(Guid compositionId)
        {
            AbortUseToolRequestPublisher.Publish(this, new AbortUseToolEventArgs(compositionId));
        }

        public void NotifyAddEffectToSelectedLayers(Guid compositionId, Guid? targetLayerId, Guid[] effectPluginIds)
        {
            AddEffectToSelectedLayersPublisher.Publish(this, new AddEffectEventArgs(compositionId, targetLayerId, effectPluginIds));
        }

        public void NotifyBeginEditDuration(Guid compositionId, Guid layerId, BeginEditDurationEventArgs.DurationType durationType)
        {
            BeginEditDurationRequestPublisher.Publish(this, new BeginEditDurationEventArgs(compositionId, layerId, durationType));
        }

        public void NotifyUpdateDuration(Guid compositionId, Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff, bool isCommit)
        {
            UpdateDurationRequestPublisher.Publish(this, new UpdateDurationEventArgs(compositionId, inPointDiff, outPointDiff, sourceStartPointDiff, isCommit));
        }

        public void NotifyAbortEditDuration(Guid compositionId)
        {
            AbortEditDurationRequestPublisher.Publish(this, new AbortEditDurationEventArgs(compositionId));
        }

        public void NotifyShowFootagePreview(Guid footageId)
        {
            ShowFootagePreviewRequestPublisher.Publish(this, new ShowFootagePreviewEventArgs(footageId));
        }

        public void NotifyPlayOrStop()
        {
            PlayOrStopRequestPublisher.Publish(this, EventArgs.Empty);
        }

        public void NotifyTextStyleChange(Guid compositionId, Guid? targetLayer, object? targetLayerPrevValue)
        {
            TextStyleChangeRequestPublisher.Publish(this, new TextStyleChangeEventArgs(compositionId, targetLayer, targetLayerPrevValue));
        }

        public void NotifyRenderPreviewInteractionRequest(Guid compositionId, Time currentTime, DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale)
        {
            RenderPreviewInteractionRequestPublisher.Publish(this, new RenderPreviewInteractionEventArgs(compositionId, currentTime, drawingContext, previewImagePosition, previewImageScale));
        }
    }
}
