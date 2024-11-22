using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Output
{
    [Export(typeof(IOutput))]
    [OutputMetadata(typeof(SequentialImageOutput), LanguageResourceDictionary.Output_SequentialImageOutput_Name, "mes51", LanguageResourceDictionary.Output_SequentialImageOutput_Description, ID, SupportedFileTypes, SourceType.Video, false, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public partial class SequentialImageOutput : IOutput
    {
        const string ID = "7982BB66-B3A5-43E9-A38E-2AF91C01912B";

        const string SupportedFileTypes = "*.png,*.bmp,*.jpg,*.gif,*.tiff";

        static readonly string[] SupportedExtensions = [..SupportedFileTypes.Split(',').Select(s => s.Replace("*", ""))];

        static readonly Regex FrameCountRegex = GenerateFrameCountRegex();

        string OutputFilePathPrefix { get; set; } = "";

        string OutputFilePathSuffix { get; set; } = "";

        string FrameCountFormat { get; set; } = "";

        string EncodeType { get; set; } = SupportedExtensions.First();

        int CurrentFrameIndex { get; set; }

        WriteableBitmap OutputImageBuffer { get; set; } = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Bgra32, null);

        int[] ImageBuffer { get; set; } = [];

        Int32Rect WriteRect { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public string ProcessOutputFilePath(string baseFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(baseFilePath);
            if (!FrameCountRegex.IsMatch(fileName))
            {
                fileName += "_[#]";
            }
            var extension = Path.GetExtension(baseFilePath);
            if (!SupportedExtensions.Contains(extension))
            {
                extension = SupportedExtensions.First();
            }

            return Path.Combine(Path.GetDirectoryName(baseFilePath) ?? "", fileName + extension);
        }

        public void BeginOutput(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            var replaceTarget = FrameCountRegex.Matches(filePath).Cast<Match>().Last();
            FrameCountFormat = "D0" + replaceTarget.Value.Replace("[", "").Replace("]", "").Length;
            OutputFilePathPrefix = filePath[..replaceTarget.Index];
            OutputFilePathSuffix = filePath[(replaceTarget.Index + replaceTarget.Length)..];
            EncodeType = Path.GetExtension(filePath);
            CurrentFrameIndex = 1;

            if (size.HasValue)
            {
                OutputImageBuffer = new WriteableBitmap(size.Value.Width, size.Value.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                if (ImageBuffer.Length > 0)
                {
                    ArrayPool<int>.Shared.Return(ImageBuffer);
                }
                ImageBuffer = ArrayPool<int>.Shared.Rent(size.Value.Width * size.Value.Height);
                WriteRect = new Int32Rect(0, 0, size.Value.Width, size.Value.Height);
            }
        }

        public void BeginPass(int pass) { }

        public int GetPassCount()
        {
            return 1;
        }

        public void ProcessFrame(int pass, double time, NImage image, bool useGpu)
        {
            var filePath = $"{OutputFilePathPrefix}{CurrentFrameIndex.ToString(FrameCountFormat)}{OutputFilePathSuffix}";
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            ImageBuffer.AsSpan().Clear();
            ImageConversion.ConvertToBGRA32(managedImage.GetDataSpan(), ImageBuffer, managedImage.DataLength);
            OutputImageBuffer.WritePixels(WriteRect, ImageBuffer, WriteRect.Width * 4, 0);

            var encoder = EncodeType switch
            {
                ".bmp" => new BmpBitmapEncoder(),
                ".jpg" => new JpegBitmapEncoder { QualityLevel = 100 },
                ".gif" => new GifBitmapEncoder(),
                ".tiff" => new TiffBitmapEncoder(),
                _ => (BitmapEncoder)new PngBitmapEncoder()
            };
            encoder.Frames.Add(BitmapFrame.Create(OutputImageBuffer));
            encoder.Save(stream);

            CurrentFrameIndex++;
        }

        public void ProcessAudio(float[] audio) { }

        public void EndPass() { }

        public void EndOutput() { }

        public void Dispose() { }

        [GeneratedRegex(@"\[#+?\]")]
        private static partial Regex GenerateFrameCountRegex();
    }
}
