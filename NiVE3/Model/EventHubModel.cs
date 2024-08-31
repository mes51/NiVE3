using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Mvvm;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class EventHubModel : BindableBase
    {
        WeakEventPublisher<SelectLayerEvent> SelectLayerRequestPublisher { get; } = new WeakEventPublisher<SelectLayerEvent>();
        public event EventHandler<SelectLayerEvent> SelectLayerRequest
        {
            add { SelectLayerRequestPublisher.Subscribe(value); }
            remove { SelectLayerRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<BeginUseToolEvent> BeginUseToolRequestPublisher { get; } = new WeakEventPublisher<BeginUseToolEvent>();
        public event EventHandler<BeginUseToolEvent> BeginUseToolRequest
        {
            add { BeginUseToolRequestPublisher.Subscribe(value); }
            remove { BeginUseToolRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<MoveLayersByToolEvent> MoveLayersByToolRequestPublisher { get; } = new WeakEventPublisher<MoveLayersByToolEvent>();
        public event EventHandler<MoveLayersByToolEvent> MoveLayersByToolRequest
        {
            add { MoveLayersByToolRequestPublisher.Subscribe(value); }
            remove { MoveLayersByToolRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<AbortUseToolEvent> AbortUseToolRequestPublisher { get; } = new WeakEventPublisher<AbortUseToolEvent>();
        public event EventHandler<AbortUseToolEvent> AbortUseToolRequest
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

        public void NotifySelectLayer(Guid compositionId, Guid? layerId)
        {
            SelectLayerRequestPublisher.Publish(this, new SelectLayerEvent(compositionId, layerId));
        }

        public void NotifyBeginUseTool(Guid compositionId, Vector2d startScreenPos, BeginUseToolEvent.PropertyType propertyType)
        {
            BeginUseToolRequestPublisher.Publish(this, new BeginUseToolEvent(compositionId, startScreenPos, propertyType));
        }

        public void NotifyMoveLayersByTool(Guid compositionId, Vector2d nextScreenPos, bool isCommit)
        {
            MoveLayersByToolRequestPublisher.Publish(this, new MoveLayersByToolEvent(compositionId, nextScreenPos, isCommit));
        }

        public void NotifyAbortUseTool(Guid compositionId)
        {
            AbortUseToolRequestPublisher.Publish(this, new AbortUseToolEvent(compositionId));
        }

        public void NotifyAddEffectToSelectedLayers(Guid compositionId, Guid effectPluginId)
        {
            AddEffectToSelectedLayersPublisher.Publish(this, new AddEffectEventArgs(compositionId, effectPluginId));
        }
    }
}
