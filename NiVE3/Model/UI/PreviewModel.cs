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
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.Model.UI
{
    [UseReactiveProperty]
    abstract partial class PreviewModelBase : BindableBase, IDisposable
    {
        public abstract bool IsFootage { get; }

        public abstract bool CanRendering { get; }

        [ReactiveProperty]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        public partial SourceType SourceType { get; set; } = SourceType.Video;

        [ReactiveProperty]
        public partial Time WorkareaBegin { get; set; }

        [ReactiveProperty]
        public partial Time WorkareaEnd { get; set; }

        [ReactiveProperty]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        public partial int Width { get; set; }

        [ReactiveProperty]
        public partial int Height { get; set; }

        [ReactiveProperty]
        public partial double DownScaleRate { get; set; } = 1.0;

        [ReactiveProperty]
        public partial bool IsLock { get; set; }

        [ReactiveProperty]
        public partial NManagedImage? SnapShotImage { get; set; }

        protected ApplicationModel ApplicationModel { get; }

        protected PreviewModelBase(ApplicationModel applicationModel)
        {
            ApplicationModel = applicationModel;
        }

        public abstract NImage? GetImage(Time time);

        public abstract float[]? GetAudio(Time time, Time length);

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

        public void Dispose()
        {
            SnapShotImage?.Dispose();
            SnapShotImage = null;
        }
    }

    [UseReactiveProperty]
    partial class FootagePreviewModel : PreviewModelBase
    {
        public override bool IsFootage => true;

        public override bool CanRendering => true;

        [ReactiveProperty]
        public partial FootageModel? Footage { get; set; }

        public FootagePreviewModel(ApplicationModel applicationModel) : base(applicationModel)
        {
            PropertyChanged += FootagePreviewModel_PropertyChanged;
        }

        public override NImage? GetImage(Time time)
        {
            if (Footage != null && (Footage.InputType.HasFlag(SourceType.Image) || Footage.InputType.HasFlag(SourceType.Video)))
            {
                return Footage.ReadImage(time, 1.0, 0, 0, null, null, ImageInterpolationQuality.Level2, false);
            }
            else
            {
                return null;
            }
        }

        public override float[]? GetAudio(Time time, Time length)
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
                    WorkareaBegin = Time.Zero;
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
                    WorkareaBegin = Time.Zero;
                    WorkareaEnd = Time.Zero;
                    Duration = Time.Zero;
                    FrameRate = 30.0;
                    DownScaleRate = 1.0;
                    Width = 0;
                    Height = 0;
                    Name = "";
                }
                CurrentTime = Time.Zero;
                OnSourceChanged();
            }
        }
    }

    class CompositionPreviewModel : PreviewModelBase
    {
        public override bool IsFootage => false;

        public override bool CanRendering => !(Composition?.IsRendering ?? true);

        public CompositionModel? Composition
        {
            get;
            set
            {
                if (field != value)
                {
                    if (field != null)
                    {
                        field.CompositionUpdated -= Composition_CompositionUpdated;
                        field.PropertyChanged -= Composition_PropertyChanged;
                    }
                    SnapShotImage?.Dispose();
                    SnapShotImage = null;
                    if (value != null)
                    {
                        value.CompositionUpdated += Composition_CompositionUpdated;
                        value.PropertyChanged += Composition_PropertyChanged;
                    }
                }
                SetProperty(ref field, value);
            }
        }

        public CompositionPreviewModel(ApplicationModel applicationModel) : base(applicationModel)
        {
            PropertyChanged += CompositionPreviewModel_PropertyChanged;
        }

        public override NImage? GetImage(Time time)
        {
            var previewImage = Composition?.RenderFrame(time, DownScaleRate, true, ApplicationModel.UseGpu);
            if (previewImage != null && DownScaleRate != 1.0)
            {
                var managedPreviewImage = previewImage.ToManaged();
                if (previewImage != managedPreviewImage)
                {
                    previewImage.Dispose();
                }

                var resizedImage = new NManagedImage(Width, Height);
                var xRate = previewImage.Width / (float)Width;
                var yRate = previewImage.Height / (float)Height;
                Parallel.For(0, Height, y =>
                {
                    var resizedImageSpan = resizedImage.GetDataSpan()[(y * Width)..];
                    var previewImageSpan = managedPreviewImage.GetDataSpan()[((int)(y * yRate) * managedPreviewImage.Width)..];

                    for (var x = 0; x < resizedImage.Width; x++)
                    {
                        resizedImageSpan[x] = previewImageSpan[(int)Math.Min(x * xRate, managedPreviewImage.Width)];
                    }
                });

                managedPreviewImage.Dispose();
                previewImage = resizedImage;
            }

            return previewImage;
        }

        public override float[]? GetAudio(Time time, Time length)
        {
            return Composition?.RenderAudio(time, length);
        }

        public void CaptureSnapShot(Time time)
        {
            if (Composition == null)
            {
                return;
            }

            var image = GetImage(time);
            if (image == null)
            {
                return;
            }

            SnapShotImage?.Dispose();
            SnapShotImage = image.ToManaged();
            if (image != SnapShotImage)
            {
                image.Dispose();
            }
        }

        private void Composition_CompositionUpdated(object? sender, NeedHistoryChangeEventArgs e)
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
                        WorkareaBegin = Time.Zero;
                        WorkareaEnd = Time.Zero;
                        Duration = Time.Zero;
                        FrameRate = 30.0;
                        DownScaleRate = 1.0;
                        Width = 0;
                        Height = 0;
                        Name = "";
                    }
                    CurrentTime = Time.Zero;
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
                case nameof(Name):
                    Name = Composition.Name;
                    break;
                case nameof(Width):
                    Width = Composition.Width;
                    break;
                case nameof(Height):
                    Height = Composition.Height;
                    break;
                case nameof(FrameRate):
                    FrameRate = Composition.FrameRate;
                    break;
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
