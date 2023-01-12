using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin;

namespace NiVE3.PresetPlugin.Effect.ExpressionControl
{
    [Export(typeof(IEffect))]
    [EffectMetadata("スライダー制御", "mes51", "エクスプレッション制御", "エクスプレッションで使用するスライダー制御", "6FA4B24F-D759-4085-90D6-EA11E537FBC0", IsDummyEffect = true)]
    public class SliderControl : IEffect
    {
    }
}
