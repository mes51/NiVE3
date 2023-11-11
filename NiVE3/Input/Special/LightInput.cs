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
    [InputMetadata(typeof(LightInput), nameof(LightInput), "", "mes51", ID, "", true)]
    [InternalInput]
    [SpecialInput]
    class LightInput : IInput
    {
        public static LightInput Instance { get; } = new LightInput();

        public string FilePath => "ライト";

        const string ID = "C425AFBF-95C8-4D6C-AD4F-0B0A2526EDEB";

        private LightInput() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup(new IFootageSource[] { EmptyFootageSource.Instance });
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }
}
