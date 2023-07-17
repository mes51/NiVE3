using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    abstract class PreviewModelBase : BindableBase
    {
        public abstract bool IsFootage { get; }

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private SourceType sourceType = SourceType.Video;
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private bool isLock;
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        public abstract NImage? GetImage(double time);
    }

    class FootagePreviewModel : PreviewModelBase
    {
        public override bool IsFootage => true;

        private FootageModel? footage;
        public FootageModel? Footage
        {
            get { return footage; }
            set { SetProperty(ref footage, value); }
        }

        public FootagePreviewModel()
        {
            PropertyChanged += FootagePreviewModel_PropertyChanged;
        }

        public override NImage? GetImage(double time)
        {
            return Footage?.ReadImage(time, false);
        }

        private void FootagePreviewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Footage))
            {
                if (Footage != null)
                {
                    SourceType = Footage.InputType;
                    Duration = Footage.Duration;
                    Width = Footage.Width;
                    Height = Footage.Height;
                    Name = Footage.Name;
                }
                else
                {
                    SourceType = SourceType.None;
                    Duration = 0.0;
                    Width = 0;
                    Height = 0;
                    Name = "";
                }
                CurrentTime = 0.0;
            }
        }
    }
}
