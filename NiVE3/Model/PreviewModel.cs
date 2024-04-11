using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Mvvm;
using NiVE3.Image;
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

        private double workareaBegin;
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
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

        protected ApplicationModel ApplicationModel { get; }

        protected PreviewModelBase(ApplicationModel applicationModel)
        {
            ApplicationModel = applicationModel;
        }

        public abstract NImage? GetImage(double time);

        public abstract float[]? GetAudio(double time, double length);

        WeakEventPublisher<EventArgs> SourceChangedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> SourceChanged
        {
            add { SourceChangedPublisher.Subscribe(value); }
            remove { SourceChangedPublisher.Unsubscribe(value); }
        }

        protected WeakEventPublisher<EventArgs> FrameUpdateRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> FrameUpdateRequest
        {
            add { FrameUpdateRequestPublisher.Subscribe(value); }
            remove { FrameUpdateRequestPublisher.Unsubscribe(value); }
        }

        protected void OnSourceChanged()
        {
            SourceChangedPublisher.Publish(this, EventArgs.Empty);
        }
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

        public FootagePreviewModel(ApplicationModel applicationModel) : base(applicationModel)
        {
            PropertyChanged += FootagePreviewModel_PropertyChanged;
        }

        public override NImage? GetImage(double time)
        {
            if (Footage != null && (Footage.InputType.HasFlag(SourceType.Image) || Footage.InputType.HasFlag(SourceType.Video)))
            {
                return Footage.ReadImage(time, 0, 0, null, ImageInterpolationQuality.Level2, false);
            }
            else
            {
                return null;
            }
        }

        public override float[]? GetAudio(double time, double length)
        {
            if (Footage != null && Footage.InputType.HasFlag(SourceType.Audio))
            {
                return Footage.ReadAudio(time, length);
            }
            else
            {
                return null;
            }
        }

        private void FootagePreviewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Footage))
            {
                if (Footage != null)
                {
                    SourceType = Footage.InputType;
                    WorkareaBegin = 0.0;
                    WorkareaEnd = Footage.Duration;
                    Duration = Footage.Duration;
                    FrameRate = Footage.InputType == SourceType.Audio ? 30.0 : Footage.FrameRate;
                    Width = Footage.Width;
                    Height = Footage.Height;
                    Name = Footage.Name;
                }
                else
                {
                    SourceType = SourceType.None;
                    WorkareaBegin = 0.0;
                    WorkareaEnd = 0.0;
                    Duration = 0.0;
                    FrameRate = 30.0;
                    Width = 0;
                    Height = 0;
                    Name = "";
                }
                CurrentTime = 0.0;
                OnSourceChanged();
            }
        }
    }

    class CompositionPreviewModel : PreviewModelBase
    {
        public override bool IsFootage => false;

        private CompositionModel? composition;
        public CompositionModel? Composition
        {
            get { return composition; }
            set
            {
                if (composition != value)
                {
                    if (composition != null)
                    {
                        composition.CompositionUpdated -= Composition_CompositionUpdated;
                        composition.PropertyChanged -= Composition_PropertyChanged;
                    }
                    if (value != null)
                    {
                        value.CompositionUpdated += Composition_CompositionUpdated;
                        value.PropertyChanged += Composition_PropertyChanged;
                    }
                }
                SetProperty(ref composition, value);
            }
        }

        public CompositionPreviewModel(ApplicationModel applicationModel) : base(applicationModel)
        {
            PropertyChanged += CompositionPreviewModel_PropertyChanged;
        }

        public override NImage? GetImage(double time)
        {
            return Composition?.RenderFrame(time, 1.0, true, ApplicationModel.UseGpu);
        }

        public override float[]? GetAudio(double time, double length)
        {
            return Composition?.RenderAudio(time, length);
        }

        private void Composition_CompositionUpdated(object? sender, EventArgs e)
        {
            FrameUpdateRequestPublisher.Publish(this, EventArgs.Empty);
        }

        private void CompositionPreviewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Composition):
                    if (Composition != null)
                    {
                        SourceType = SourceType.VideoAndAudio;
                        WorkareaBegin = Composition.WorkareaBegin;
                        WorkareaEnd = Composition.WorkareaEnd;
                        Duration = Composition.Duration;
                        FrameRate = Composition.FrameRate;
                        Width = Composition.Width;
                        Height = Composition.Height;
                        Name = Composition.Name;
                    }
                    else
                    {
                        SourceType = SourceType.None;
                        WorkareaBegin = 0.0;
                        WorkareaEnd = 0.0;
                        Duration = 0.0;
                        FrameRate = 30.0;
                        Width = 0;
                        Height = 0;
                        Name = "";
                    }
                    CurrentTime = 0.0;
                    OnSourceChanged();
                    break;
                case nameof(CurrentTime) when Composition != null:
                    Composition.CurrentTime = CurrentTime;
                    break;
            }
        }

        private void Composition_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Composition == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(CompositionModel.WorkareaBegin):
                    WorkareaBegin = Composition.WorkareaBegin;
                    break;
                case nameof(CompositionModel.WorkareaEnd):
                    WorkareaEnd = Composition.WorkareaEnd;
                    break;
                case nameof(CompositionModel.Duration):
                    Duration = Composition.Duration;
                    break;
            }
        }
    }
}
