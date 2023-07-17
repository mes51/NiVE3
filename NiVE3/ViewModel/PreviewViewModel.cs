using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Dock;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PreviewViewModel : PaneViewModelBase
    {
        private string name = "";
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isFootage;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public bool IsFootage
        {
            get { return isFootage; }
            set { SetProperty(ref isFootage, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private double duration;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double currentTime;
        [NeedWire(nameof(PreviewModel))]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private int width;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private bool isLock;
        [NeedWire(nameof(PreviewModel))]
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private double timeBarRange;
        public double TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private double timeBarRangeStart;
        public double TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }

        public WriteableBitmap CurrentFrame { get; set; }

        PreviewModelBase PreviewModel { get; }

        public PreviewViewModel(PreviewModelBase previewModel)
        {
            PreviewModel = previewModel;

            WiringModel();

            Title = $"{(IsFootage ? "フッテージ" : "コンポジション")} {(SourceType != SourceType.None ? Name : "(なし)")}";
            TimeBarRange = previewModel.Duration;

            CurrentFrame = new WriteableBitmap(Math.Max(Width, 1), Math.Max(Height, 1), 96.0, 96.0, PixelFormats.Bgra32, null);
            PropertyChanged += PreviewViewModel_PropertyChanged;
        }

        partial void WiringModel();

        void UpdateCurrentFrame()
        {
            using var image = PreviewModel.GetImage(CurrentTime);
            if (image != null && CurrentFrame.PixelWidth == image.Width && CurrentFrame.PixelHeight == image.Height)
            {
                var dataSize = image.DataLength;
                var floatData = image.GetData();
                var data = ArrayPool<byte>.Shared.Rent(dataSize);
                for (var i = 0; i < dataSize; i++)
                {
                    // TODO: SDR変換を入れるかどうか
                    data[i] = (byte)MathF.Round(floatData[i] * 255.0F);
                }
                CurrentFrame.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), data, image.Width * 4, 0);
                ArrayPool<byte>.Shared.Return(data);
            }
            else
            {
                var data = ArrayPool<byte>.Shared.Rent(CurrentFrame.PixelWidth * CurrentFrame.PixelHeight * 4);
                CurrentFrame.WritePixels(new Int32Rect(0, 0, CurrentFrame.PixelWidth, CurrentFrame.PixelHeight), data, CurrentFrame.PixelWidth * 4, 0);
                ArrayPool<byte>.Shared.Return(data);
            }
            RaisePropertyChanged(nameof(CurrentFrame));
        }

        private void PreviewViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SourceType):
                case nameof(Name):
                    Title = $"{(IsFootage ? "フッテージ" : "コンポジション")} {(SourceType != SourceType.None ? Name : "(なし)")}";
                    break;
                case nameof(Width):
                case nameof(Height):
                    CurrentFrame = new WriteableBitmap(Math.Max(Width, 1), Math.Max(Height, 1), 96.0, 96.0, PixelFormats.Bgra32, null);
                    UpdateCurrentFrame();
                    break;
                case nameof(CurrentTime):
                    UpdateCurrentFrame();
                    break;
                case nameof(Duration):
                    TimeBarRange = Duration;
                    TimeBarRangeStart = 0.0;
                    break;
            }
        }
    }
}
