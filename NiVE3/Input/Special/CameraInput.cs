using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input.Special
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(CameraInput), nameof(CameraInput), "", "mes51", ID, "", true)]
    [InternalInput]
    [SpecialInput]
    class CameraInput : IInput
    {
        const string ID = "69EEC338-926A-413D-BE13-7900F711F21C";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public static CameraInput Instance { get; } = new CameraInput();

        public string FilePath => "カメラ";

        private CameraInput() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup([EmptyFootageSource.Instance]);
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }
}
