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
using NiVE3.Data;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Input;
using NiVE3.ViewModel.Input;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(SolidInput), "SolidInput", "", "mes51", ID, "", true)]
    [InternalInput]
    class SolidInput : IInput
    {
        const string ID = "08FE7B2F-4FC5-419B-8CE0-A9262348D630";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public string FilePath { get; private set; } = "平面";

        SolidFootageSource Source { get; } = new SolidFootageSource();

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public bool Load(string filePath)
        {
            // TODO
            Source.Width = 1920;
            Source.Height = 1080;

            return true;
        }

        public void Dispose() { }

        public object? SaveData()
        {
            return new SolidData
            {
                Color = Source.Color,
                Width = Source.Width,
                Height = Source.Height
            };
        }

        public bool LoadData(object? data)
        {
            if (data is not IDictionary<string, object> solidData)
            {
                return false;
            }

            var colorData = (IDictionary<string, object>)solidData[nameof(SolidData.Color)];
            Source.Color = new FloatColor(
                Convert.ToSingle(colorData[nameof(FloatColor.R)]),
                Convert.ToSingle(colorData[nameof(FloatColor.G)]),
                Convert.ToSingle(colorData[nameof(FloatColor.B)]),
                Convert.ToSingle(colorData[nameof(FloatColor.A)])
            );
            Source.Width = Convert.ToInt32(solidData[nameof(SolidData.Width)]);
            Source.Height = Convert.ToInt32(solidData[nameof(SolidData.Height)]);
            return true;
        }

        public FrameworkElement? GetLoadSetting(Size? compositionSize)
        {
            return new SolidSettingView { DataContext = new SolidSettingViewModel() };
        }

        public bool ApplyLoadSetting(object? setting)
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
            return new FootageSourceGroup(new IFootageSource[] { Source });
        }
    }

    class SolidFootageSource : IFootageSource
    {
        public string SourceId => "Solid";

        public double FrameRate => 0.0;

        public double Duration => 0.0;

        public int Width { get; internal set; }

        public int Height { get; internal set; }

        public SourceType SourceType => SourceType.Image;

        internal FloatColor Color { get; set; } = new FloatColor(0.0F, 0.0F, 1.0F, 1.0F);

        public NImage ReadFrame(double time, bool toGpu)
        {
            if (toGpu)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var image = new NManagedImage(Width, Height, true);
                image.GetDataSpan().Fill((Vector4)Color);
                return image;
            }
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }
    }

    file class SolidData
    {
        public FloatColor Color { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
