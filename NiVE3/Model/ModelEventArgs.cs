using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
}
