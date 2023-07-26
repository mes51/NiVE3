using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata("デフォルトレンダラ", "NiVE標準のレンダラ", "mes51", "D67AC3F-A137-45B1-99F7-3E68A0B910E6")]
    public class DefaultRenderer : IRenderer
    {
    }
}
