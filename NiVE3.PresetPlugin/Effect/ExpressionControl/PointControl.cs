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
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_PointControl_Name, "mes51", "エクスプレッション制御", LanguageResourceDictionary.ExpressionControl_PointControl_Description, "6836A601-35DC-405D-8D77-D6DC52A36845", IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class PointControl : IEffect
    {
    }
}
