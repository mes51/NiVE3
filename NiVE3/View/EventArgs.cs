using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Property;

namespace NiVE3.View
{
    class KeyFrameMoveEventArgs : RoutedEventArgs
    {
        public KeyFrame[] KeyFrames { get; }

        public double[] NewTimes { get; }

        public KeyFrameMoveEventArgs(KeyFrame[] keyFrames, double[] newTimes, RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
            KeyFrames = keyFrames;
            NewTimes = newTimes;
        }
    }
}
