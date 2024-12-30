using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal.Encoder;
using NiVE3.PresetPlugin.Internal.Mvvm;
using NiVE3.PresetPlugin.Output;
using NiVE3.Shared.Extension;
using NiVE3.UI.Command;
using SharpAvi;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    class AviOutputSettingViewModel : BindableBase
    {
        private bool supportQuality;
        public bool SupportQuality
        {
            get { return supportQuality; }
            set { SetProperty(ref supportQuality, value); }
        }

        private bool supportKeyFrameRate;
        public bool SupportKeyFrameRate
        {
            get { return supportKeyFrameRate; }
            set { SetProperty(ref supportKeyFrameRate, value); }
        }

        private bool hasConfigure;
        public bool HasConfigure
        {
            get { return hasConfigure; }
            set { SetProperty(ref hasConfigure, value); }
        }

        private int quality;
        public int Quality
        {
            get { return quality; }
            set { SetProperty(ref quality, value); }
        }

        private bool useKeyFrameRate;
        public bool UseKeyFrameRate
        {
            get { return useKeyFrameRate; }
            set { SetProperty(ref useKeyFrameRate, value); }
        }

        private int keyFrameRate;
        public int KeyFrameRate
        {
            get { return keyFrameRate; }
            set { SetProperty(ref keyFrameRate, value); }
        }

        private int selectedCodecIndex;
        public int SelectedCodecIndex
        {
            get { return selectedCodecIndex; }
            set { SetProperty(ref selectedCodecIndex, value); }
        }

        private OutputChannel outputChannel;
        public OutputChannel OutputChannel
        {
            get { return outputChannel; }
            set { SetProperty(ref outputChannel, value); }
        }

        private OutputAlphaMode outputAlphaMode;
        public OutputAlphaMode OutputAlphaMode
        {
            get { return outputAlphaMode; }
            set { SetProperty(ref outputAlphaMode, value); }
        }

        private ObservableCollection<Tuple<FourCC, string>> codecList = [];
        public ObservableCollection<Tuple<FourCC, string>> CodecList
        {
            get { return codecList; }
            set { SetProperty(ref codecList, value); }
        }

        private byte[]? codecState;
        public byte[]? CodecState
        {
            get { return codecState; }
            set { SetProperty(ref codecState, value); }
        }

        private int audioSamplingRate;
        public int AudioSamplingRate
        {
            get { return audioSamplingRate; }
            set { SetProperty(ref audioSamplingRate, value); }
        }

        private int audioBitsPerSample;
        public int AudioBitsPerSample
        {
            get { return audioBitsPerSample; }
            set { SetProperty(ref audioBitsPerSample, value); }
        }

        public FourCC Codec
        {
            get => CodecList[SelectedCodecIndex].Item1;
            set
            {
                SelectedCodecIndex = Math.Max(CodecList.FindIndex(t => t.Item1 == value), 0);
            }
        }

        public bool HasVideo { get; }

        public bool HasAudio { get; }

        public ICommand OpenCodecConfigureCommand { get; }

        int Width { get; }

        int Height { get; }

        public AviOutputSettingViewModel(int width, int height, OutputChannel outputChannel, SourceType outputSources)
        {
            Width = width;
            Height = height;
            HasVideo = outputSources.HasFlag(SourceType.Video);
            HasAudio = outputSources.HasFlag(SourceType.Audio);
            OutputChannel = outputChannel;
            UpdateCodecList();

            OpenCodecConfigureCommand = new RequerySuggestedCommand<DependencyObject>(ui =>
            {
                var codec = CodecList[SelectedCodecIndex].Item1;
                using var configurator = new CompressorConfigurator(codec, Width, Height, OutputChannel.ToBitsPerPixel());
                var ownerWindow = Window.GetWindow(ui);
                if (CodecState != null)
                {
                    configurator.SetState(CodecState);
                }
                configurator.OpenConfig(new WindowInteropHelper(ownerWindow).Handle);
                CodecState = configurator.GetState();
            }, _ => HasConfigure);

            PropertyChanged += AviOutputSettingViewModel_PropertyChanged;
        }

        private void AviOutputSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(OutputChannel):
                    UpdateCodecList();
                    break;
                case nameof(SelectedCodecIndex) when SelectedCodecIndex > -1: // NOTE: コーデックリスト更新中に-1になることがあるので無視する
                    {
                        var codec = CodecList[SelectedCodecIndex].Item1;
                        if (codec == 0)
                        {
                            CodecState = null;
                            SupportQuality = false;
                            SupportKeyFrameRate = false;
                            KeyFrameRate = 1;
                            HasConfigure = false;
                        }
                        else
                        {
                            using var configurator = new CompressorConfigurator(codec, Width, Height, OutputChannel.ToBitsPerPixel());
                            CodecState = null;
                            SupportQuality = configurator.SupportQuality;
                            SupportKeyFrameRate = configurator.SupportKeyFrameRate;
                            KeyFrameRate = configurator.DefaultKeyFrameRate;
                            HasConfigure = configurator.HasConfigure;
                        }
                    }
                    break;
            }
        }

        void UpdateCodecList()
        {
            if (!HasVideo)
            {
                return;
            }

            var selectedCodec = CodecList.Count > 0 ? CodecList[SelectedCodecIndex].Item1 : new FourCC(0);
            var newCodecList = new ObservableCollection<Tuple<FourCC, string>>();
            var bpc = OutputChannel.ToBitsPerPixel();
            foreach (var t in CompressorConfigurator.GetSupportedCodec(Width, Height, bpc))
            {
                newCodecList.Add(t);
            }

            CodecList = newCodecList;
            SelectedCodecIndex = Math.Max(newCodecList.FindIndex(t => t.Item1 == selectedCodec), 0);
        }
    }
}
