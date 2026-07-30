using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Model
{
    /// <summary>
    /// エフェクトの実体とその生存期間を抽象化します
    /// (MEF 由来か OpenFX 由来かを EffectModel から隠蔽するための内部インターフェース)
    /// </summary>
    interface IEffectHandle : IDisposable
    {
        IEffect Value { get; }
    }

    /// <summary>
    /// MEF の ExportLifetimeContext をラップする IEffectHandle
    /// </summary>
    sealed class MefEffectHandle : IEffectHandle
    {
        public IEffect Value => Context.Value;

        ExportLifetimeContext<IEffect> Context { get; }

        public MefEffectHandle(ExportLifetimeContext<IEffect> context)
        {
            Context = context;
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }

    /// <summary>
    /// IEffect の実体を直接保持する IEffectHandle (OpenFX アダプタなど MEF を経由しないエフェクト用)
    /// </summary>
    sealed class DirectEffectHandle : IEffectHandle
    {
        public IEffect Value { get; }

        public DirectEffectHandle(IEffect effect)
        {
            Value = effect;
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
