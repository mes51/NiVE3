using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "D67AC3F-A137-45B1-99F7-3E68A0B910E6", LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class DefaultRenderer : IRenderer
    {
        public void SetupAccelerator(Accelerator? accelerator) { }

        public void Dispose()
        {
        }
    }
}
