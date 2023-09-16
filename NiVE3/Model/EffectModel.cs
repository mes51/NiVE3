using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class EffectModel : BindableBase, IDisposable, IEffectObject
    {
        public IEffect Effect { get; }

        public EffectModel(IEffect effect)
        {
            Effect = effect;
        }

        public void Dispose() { }
    }
}
