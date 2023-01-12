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
    [EffectMetadata("ポイント制御", "mes51", "エクスプレッション制御", "エクスプレッションで使用するポイント制御", "6836A601-35DC-405D-8D77-D6DC52A36845", IsDummyEffect = true)]
    public class PointControl : IEffect
    {
    }
}
