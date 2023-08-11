using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Util;
using NiVE3.View.Dock;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PreviewViewModel : PaneViewModelBase
    {
        const int Black = 255 << 24;

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

        private double scale = 100.0;
        public double Scale
        {
            get { return scale; }
            set { SetProperty(ref scale, value); }
        }

        private bool isStretchPreview;
        public bool IsStretchPreview
        {
            get { return isStretchPreview; }
            set { SetProperty(ref isStretchPreview, value); }
        }

        private bool isStretchLimited;
        public bool IsStretchLimited
        {
            get { return isStretchLimited; }
            set { SetProperty(ref isStretchLimited, value); }
        }

        private int downScaleRate;
        public int DownScaleRate
        {
            get { return downScaleRate; }
            set { SetProperty(ref downScaleRate, value); }
        }

        private PreviewColorChannel previewColorChannel = PreviewColorChannel.Rgb;
        public PreviewColorChannel PreviewColorChannel
        {
            get { return previewColorChannel; }
            set { SetProperty(ref previewColorChannel, value); }
        }

        public WriteableBitmap CurrentFrame { get; set; }

        PreviewModelBase PreviewModel { get; }

        public event EventHandler? SourceChanged;

        byte[] Buffer { get; set; }

        Int32Rect BufferImageSize { get; set; }

        bool IsDirtyBuffer { get; set; }

        bool NeedUpdateFrameNextTick { get; set; }

        Debouncer FrameUpdateDebouncer { get; }

        public PreviewViewModel(PreviewModelBase previewModel)
        {
            PreviewModel = previewModel;

            WiringModel();

            Title = $"{LanguageResourceDictionary.Dictionary.GetText(IsFootage ? LanguageResourceDictionary.PreviewView_FootageTitle : LanguageResourceDictionary.PreviewView_CompositionTitle)} {(SourceType != SourceType.None ? Name : LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PreviewView_Title_ItemEmpty))}";
            TimeBarRange = previewModel.Duration;

            BufferImageSize = new Int32Rect(0, 0, Math.Max(Width, 1), Math.Max(Height, 1));
            CurrentFrame = new WriteableBitmap(BufferImageSize.Width, BufferImageSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            PropertyChanged += PreviewViewModel_PropertyChanged;

            Buffer = new byte[BufferImageSize.Width * BufferImageSize.Height * 4];
            FrameUpdateDebouncer = new Debouncer(1);
            FrameUpdateDebouncer.Tick += (_, _) =>
            {
                UpdateCurrentFrame();
            };

            CompositionTarget.Rendering += (_, _) =>
            {
                if (IsDirtyBuffer)
                {
                    CurrentFrame.WritePixels(BufferImageSize, Buffer, BufferImageSize.Width * 4, 0);
                    IsDirtyBuffer = false;
                    if (NeedUpdateFrameNextTick)
                    {
                        FrameUpdateDebouncer.ResetAndStart();
                    }
                }
            };
        }

        partial void WiringModel();

        void UpdateCurrentFrame()
        {
            if (IsDirtyBuffer)
            {
                NeedUpdateFrameNextTick = true;
                return;
            }

            using var image = PreviewModel.GetImage(CurrentTime);
            if (image != null && BufferImageSize.Width == image.Width && BufferImageSize.Height == image.Height)
            {
                var dataSize = image.DataLength;
                var floatData = image.GetData();
                var data = Buffer;

                // TODO: SDR変換を入れるかどうか
                switch (PreviewColorChannel)
                {
                    case PreviewColorChannel.R:
                        for (var i = 0; i < dataSize; i += 4)
                        {
                            data[i] = data[i + 1] = data[i + 2] = (byte)MathF.Round(floatData[i + 2] * 255.0F);
                            data[i + 3] = 255;
                        }
                        break;
                    case PreviewColorChannel.G:
                        for (var i = 0; i < dataSize; i += 4)
                        {
                            data[i] = data[i + 1] = data[i + 2] = (byte)MathF.Round(floatData[i + 1] * 255.0F);
                            data[i + 3] = 255;
                        }
                        break;
                    case PreviewColorChannel.B:
                        for (var i = 0; i < dataSize; i += 4)
                        {
                            data[i] = data[i + 1] = data[i + 2] = (byte)MathF.Round(floatData[i] * 255.0F);
                            data[i + 3] = 255;
                        }
                        break;
                    case PreviewColorChannel.Alpha:
                        for (var i = 0; i < dataSize; i += 4)
                        {
                            data[i] = data[i + 1] = data[i + 2] = (byte)MathF.Round(floatData[i + 3] * 255.0F);
                            data[i + 3] = 255;
                        }
                        break;
                    case PreviewColorChannel.RgbStraight:
                        for (var i = 0; i < dataSize; i += 4)
                        {
                            data[i] = (byte)MathF.Round(floatData[i] * 255.0F);
                            data[i + 1] = (byte)MathF.Round(floatData[i + 1] * 255.0F);
                            data[i + 2] = (byte)MathF.Round(floatData[i + 2] * 255.0F);
                            data[i + 3] = 255;
                        }
                        break;
                    default:
                        for (var i = 0; i < dataSize; i++)
                        {
                            data[i] = (byte)MathF.Round(floatData[i] * 255.0F);
                        }
                        break;
                }
            }
            else
            {
                if (PreviewColorChannel != PreviewColorChannel.Rgb)
                {
                    MemoryMarshal.Cast<byte, int>(Buffer).Fill(Black);
                }
                else
                {
                    Buffer.AsSpan(0).Fill(0);
                }
            }
            IsDirtyBuffer = true;
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
                    BufferImageSize = new Int32Rect(0, 0, Math.Max(Width, 1), Math.Max(Height, 1));
                    CurrentFrame = new WriteableBitmap(BufferImageSize.Width, BufferImageSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                    Buffer = new byte[BufferImageSize.Width * BufferImageSize.Height * 4];
                    UpdateCurrentFrame();
                    break;
                case nameof(CurrentTime):
                    UpdateCurrentFrame();
                    break;
                case nameof(Duration):
                    TimeBarRange = Duration;
                    TimeBarRangeStart = 0.0;
                    break;
                case nameof(PreviewColorChannel):
                    UpdateCurrentFrame();
                    break;
            }
        }

        [BindWeakEvent(nameof(PreviewModel), nameof(PreviewModelBase.SourceChanged))]
        private void PreviewModel_SourceChanged(object? sender, EventArgs e)
        {
            Scale = 100.0;
            IsStretchPreview = false;
            IsStretchLimited = false;
            DownScaleRate = 1;
            previewColorChannel = PreviewColorChannel.Rgb;
            UpdateCurrentFrame();
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    enum PreviewColorChannel
    {
        Rgb,
        R,
        G,
        B,
        Alpha,
        RgbStraight
    }
}
