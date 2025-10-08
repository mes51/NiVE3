using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using ComputeSharp;
using NiVE3.Data;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Input;
using NiVE3.ViewModel.Input;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(SolidInput), "SolidInput", "", "mes51", ID, "", true, IsSupportLoadToGpu = true)]
    [InternalInput]
    class SolidInput : IInput
    {
        const string ID = "08FE7B2F-4FC5-419B-8CE0-A9262348D630";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public string FilePath { get; private set; } = "平面";

        SolidFootageSource Source { get; } = new SolidFootageSource();

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            Source.Accelerator = accelerator;
        }

        public bool Load(string filePath)
        {
            // TODO
            Source.Width = 1920;
            Source.Height = 1080;

            return true;
        }

        public void Dispose() { }

        public object? SaveSetting()
        {
            return new SolidData
            {
                Name = FilePath,
                Color = Source.Color,
                Width = Source.Width,
                Height = Source.Height
            };
        }

        public bool LoadSetting(object? data)
        {
            if (data is not IDictionary<string, object> solidData ||
                !solidData.TryGetValue(nameof(SolidData.Name), out var nameValue) || nameValue is not string name ||
                !solidData.TryGetValue(nameof(SolidData.Color), out var colorDataValue) || colorDataValue is not IDictionary<string, object> colorData ||
                !solidData.TryGetValue(nameof(SolidData.Width), out var widthValue) ||
                !solidData.TryGetValue(nameof(SolidData.Height), out var heightValue))
            {
                return false;
            }

            FilePath = !string.IsNullOrEmpty(name) ? name : FilePath;
            Source.Color = new FloatColor(
                Convert.ToSingle(colorData[nameof(FloatColor.R)]),
                Convert.ToSingle(colorData[nameof(FloatColor.G)]),
                Convert.ToSingle(colorData[nameof(FloatColor.B)]),
                Convert.ToSingle(colorData[nameof(FloatColor.A)])
            );
            Source.Width = Convert.ToInt32(widthValue);
            Source.Height = Convert.ToInt32(heightValue);
            return true;
        }

        public FrameworkElement? GetLoadSetting(Int32Size? compositionSize)
        {
            return new SolidSettingView { DataContext = new SolidSettingViewModel(compositionSize) };
        }

        public bool ApplySetting(object? setting)
        {
            if (setting is SolidSettingViewModel vm)
            {
                FilePath = vm.Name;
                Source.Width = vm.Width;
                Source.Height = vm.Height;
                Source.Color = vm.Color;
                return true;
            }
            else
            {
                return false;
            }
        }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup([Source]);
        }
    }

    class SolidFootageSource : IFootageSource
    {
        public string SourceId => "Solid";

        public string? Name => null;

        public double FrameRate => 0.0;

        public Time Duration => Time.Zero;

        public int Width { get; internal set; }

        public int Height { get; internal set; }

        public SourceType SourceType => SourceType.Image;

        internal FloatColor Color { get; set; } = new FloatColor(0.0F, 0.0F, 1.0F, 1.0F);

        internal IAcceleratorObject? Accelerator { get; set; }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            var width = (int)(Width / downSamplingRate);
            var height = (int)(Height / downSamplingRate);
            if (toGpu && Accelerator != null)
            {
                return new NGPUImage(width, height, Accelerator.CurrentDevice, (Vector4)Color);
            }
            else
            {
                var image = new NManagedImage(width, height, true);
                image.GetDataSpan().Fill((Vector4)Color);
                return image;
            }
        }

        public float[] ReadAudio(Time time, Time length)
        {
            throw new NotImplementedException();
        }
    }

    file class SolidData
    {
        public string Name { get; set; } = "";

        public FloatColor Color { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
