using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Resource;

namespace NiVE3.ToneMapper
{
    [Export(typeof(IToneMapper))]
    [ToneMapperMetadata(typeof(NoOpToneMapper), LanguageResourceDictionary.ToneMapper_NoOpToneMapper_Name, "mes51", LanguageResourceDictionary.ToneMapper_NoOpToneMapper_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    class NoOpToneMapper : IToneMapper
    {
        const string ID = "D20F11E2-FDC8-482D-8730-30C517811176";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public NImage ToneMapping(NImage image, bool useGpu)
        {
            return image;
        }

        public void Dispose() { }
    }
}
