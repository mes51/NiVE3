using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.ExpressionControl
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_SliderControl_Name, "mes51", "エクスプレッション制御", LanguageResourceDictionary.ExpressionControl_SliderControl_Description, "6FA4B24F-D759-4085-90D6-EA11E537FBC0", IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class SliderControl : IEffect
    {
    }
}
