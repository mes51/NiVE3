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
    [InputMetadata(typeof(NullObjectInput), nameof(NullObjectInput), "", "mes51", ID, "", true)]
    [InternalInput]
    [SpecialInput]
    class NullObjectInput : IInput
    {
        const string ID = "6F491207-1430-4E92-8D09-08BA2BCBD557";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public static NullObjectInput Instance { get; } = new NullObjectInput();

        public string FilePath => "ヌルオブジェクト";

        private NullObjectInput() { }

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
