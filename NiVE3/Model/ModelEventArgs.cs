using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;

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

    class SelectLayerEvent : EventArgs
    {
        public Guid CompositionId { get; }

        public Guid? LayerId { get; }

        public SelectLayerEvent(Guid compositionId, Guid? layerId)
        {
            CompositionId = compositionId;
            LayerId = layerId;
        }
    }

    class BeginUseToolEvent : EventArgs
    {
        public Guid CompositionId { get; }

        public Vector2d StartScreenPosition { get; }

        public string PropertyName { get; }

        public BeginUseToolEvent(Guid compositionId, Vector2d startScreenPosition, string propertyName)
        {
            CompositionId = compositionId;
            StartScreenPosition = startScreenPosition;
            PropertyName = propertyName;
        }
    }

    class MoveLayersByToolEvent : EventArgs
    {
        public Guid CompositionId { get; }

        public Vector2d NextScreenPos { get; }

        public bool IsCommit { get; }

        public MoveLayersByToolEvent(Guid compositionId, Vector2d nextScreenPos, bool isCommit)
        {
            CompositionId = compositionId;
            NextScreenPos = nextScreenPos;
            IsCommit = isCommit;
        }
    }

    class AbortUseToolEvent : EventArgs
    {
        public Guid CompositionId { get; }

        public AbortUseToolEvent(Guid compositionId)
        {
            CompositionId = compositionId;
        }
    }
}
