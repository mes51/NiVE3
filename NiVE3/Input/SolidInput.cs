using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ILGPU.Runtime;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata("SolidInput", "mes51", ID, "", true)]
    [InternalInput]
    class SolidInput : IInput
    {
        const string ID = "08FE7B2F-4FC5-419B-8CE0-A9262348D630";

        public string FilePath => "";

        public double FrameRate => 0.0;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public void Load(string filePath)
        {
            // TODO
            Width = 1920;
            Height = 1080;
        }

        public NImage Read(double time, bool toGpu)
        {
            if (toGpu)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var image = new NManagedImage(Width, Height, true);
                var span = image.Data.AsSpan(0, image.DataLength);
                MemoryMarshal.Cast<float, Vector128<float>>(span).Fill(Vector128.Create(0.0F, 0.0F, 1.0F, 1.0F));
                return image;
            }
        }

        public void Dispose() { }

        public Window? ShowLoadSetting()
        {
            return null; // TODO
        }
    }
}
