using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Psd;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(PsdInput), "PsdInput", "", "mes51", ID, "*.psd,*.psb", IsSupportLoadToGpu = false)]
    public sealed class PsdInput : IInput
    {
        const string ID = "BEFBFF5B-734D-49C7-8283-D09E028DCEA2";

        public string FilePath { get; private set; } = "";

        PsdFile? PsdFile { get; set; }

        NManagedImage? CompositedImage { get; set; }

        public FootageSourceGroup GetGroup()
        {
            if (CompositedImage != null)
            {
                return new FootageSourceGroup([new SingleImageFootageSource(CompositedImage)]);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public bool Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            FilePath = filePath;

            try
            {
                PsdFile = PsdFile.Parse(filePath);
            }
            catch
            {
                return false;
            }

            CompositedImage = PsdFile.ReadCompositedImage() ?? PsdFile.ReadImageData();
            if (CompositedImage == null)
            {
                return false;
            }

            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose()
        {
            CompositedImage?.Dispose();
        }
    }
}
