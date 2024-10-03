using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Mvvm;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
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

        public void NotifySelectLayer(Guid compositionId, Guid? layerId)
        {
            SelectLayerRequestPublisher.Publish(this, new SelectLayerEventArgs(compositionId, layerId));
        }

        public void NotifyBeginUseTool(Guid compositionId, Vector2d startScreenPos, BeginUseToolEventArgs.PropertyType propertyType)
        {
            BeginUseToolRequestPublisher.Publish(this, new BeginUseToolEventArgs(compositionId, startScreenPos, propertyType));
        }

        public void NotifyMoveLayersByTool(Guid compositionId, Vector2d nextScreenPos, bool isCommit)
        {
            MoveLayersByToolRequestPublisher.Publish(this, new MoveLayersByToolEventArgs(compositionId, nextScreenPos, isCommit));
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

        public void NotifyUpdateDuration(Guid compositionId, double inPointDiff, double outPointDiff, double sourceStartPointDiff, bool isCommit)
        {
            UpdateDurationRequestPublisher.Publish(this, new UpdateDurationEventArgs(compositionId, inPointDiff, outPointDiff, sourceStartPointDiff, isCommit));
        }

        public void NotifyAbortEditDuration(Guid compositionId)
        {
            AbortEditDurationRequestPublisher.Publish(this, new AbortEditDurationEventArgs(compositionId));
        }
    }
}
