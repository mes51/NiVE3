using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class CompositionModel : BindableBase
    {
        string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width = 1920;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height = 1080;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double frameRate = 30.0;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double frameDuration = 1.0 / 30.0;
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double duration = 10.0;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private bool isRetentionFrameRate;
        public bool IsRetentionFrameRate
        {
            get { return isRetentionFrameRate; }
            set { SetProperty(ref isRetentionFrameRate, value); }
        }

        private int shutterAngle = 180;
        public int ShutterAngle
        {
            get { return shutterAngle; }
            set { SetProperty(ref shutterAngle, value); }
        }

        private int shutterPhase = 180;
        public int ShutterPhase
        {
            get { return shutterPhase; }
            set { SetProperty(ref shutterPhase, value); }
        }

        private int motionBlurSampleCount = 16;
        public int MotionBlurSampleCount
        {
            get { return motionBlurSampleCount; }
            set { SetProperty(ref motionBlurSampleCount, value); }
        }

        private ObservableCollection<LayerModel> layers = new ObservableCollection<LayerModel>();
        public ObservableCollection<LayerModel> Layers
        {
            get { return layers; }
            set
            {
                if (layers != value)
                {
                    layers.CollectionChanged -= Layers_CollectionChanged;
                    value.CollectionChanged += Layers_CollectionChanged;
                }
                SetProperty(ref layers, value);
            }
        }

        IRenderer Renderer { get; }

        public CompositionModel(IRenderer renderer)
        {
            Renderer = renderer;
            Layers = new ObservableCollection<LayerModel>();
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
