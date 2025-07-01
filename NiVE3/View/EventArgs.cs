using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.ValueObject;

namespace NiVE3.View
{
    class KeyFrameMoveEventArgs : RoutedEventArgs
    {
        public KeyFrame[] KeyFrames { get; }

        public Time[] NewTimes { get; }

        public KeyFrameMoveEventArgs(KeyFrame[] keyFrames, Time[] newTimes, RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
            KeyFrames = keyFrames;
            NewTimes = newTimes;
        }
    }

    class ChangeKeyFrameInterpolationTypeEventArgs : RoutedEventArgs
    {
        public KeyFrame[] KeyFrames { get; }

        public InterpolationType InterpolationType { get; }

        public ChangeKeyFrameInterpolationTypeEventArgs(KeyFrame[] keyFrames, InterpolationType interpolationType, RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
            KeyFrames = keyFrames;
            InterpolationType = interpolationType;
        }
    }

    class MarkerMoveEventArgs : RoutedEventArgs
    {
        public Marker Marker { get; }

        public Time NewTime { get; }

        public MarkerMoveEventArgs(Marker marker, Time newTime, RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
            Marker = marker;
            NewTime = newTime;
        }
    }
}
