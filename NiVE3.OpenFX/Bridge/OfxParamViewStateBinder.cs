using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Host;
using NiVE3.OpenFX.Interop;
using NiVE3.Plugin.Property;

namespace NiVE3.OpenFX.Bridge
{
    /// <summary>
    /// OFX パラメータの Secret / Enabled プロパティと NiVE3 の PropertyViewState を結び付け、
    /// プラグインによる実行時の表示/有効状態の変更 (InstanceChanged 中の propSet など) を UI へ反映します
    /// </summary>
    public sealed class OfxParamViewStateBinder
    {
        Dictionary<ParamInstance, List<WeakReference<PropertyViewState>>> States { get; } = new Dictionary<ParamInstance, List<WeakReference<PropertyViewState>>>();

        object Lock { get; } = new object();

        /// <summary>
        /// パラメータに連動する PropertyViewState を生成します
        /// </summary>
        /// <param name="param">対象のパラメータ</param>
        /// <param name="displayName">表示名</param>
        /// <returns>生成されたステート</returns>
        public PropertyViewState CreateState(ParamInstance param, string displayName)
        {
            var state = new PropertyViewState(displayName, IsEnabled(param), IsVisible(param));
            lock (Lock)
            {
                if (!States.TryGetValue(param, out var list))
                {
                    list = [];
                    States[param] = list;
                    param.UiStateChanged += Param_UiStateChanged;
                }
                list.Add(new WeakReference<PropertyViewState>(state));
            }
            return state;
        }

        void Param_UiStateChanged(ParamInstance param)
        {
            var enabled = IsEnabled(param);
            var visible = IsVisible(param);

            List<PropertyViewState> targets = new List<PropertyViewState>();
            lock (Lock)
            {
                if (!States.TryGetValue(param, out var list))
                {
                    return;
                }
                list.RemoveAll(reference => !reference.TryGetTarget(out _));
                foreach (var reference in list)
                {
                    if (reference.TryGetTarget(out var state))
                    {
                        targets.Add(state);
                    }
                }
            }

            foreach (var state in targets)
            {
                state.IsEnabled = enabled;
                state.IsVisible = visible;
            }
        }

        static bool IsEnabled(ParamInstance param)
        {
            return param.Properties.GetOrDefault(OfxNames.ParamPropEnabled, 0) is not int enabled || enabled != 0;
        }

        static bool IsVisible(ParamInstance param)
        {
            return param.Properties.GetOrDefault(OfxNames.ParamPropSecret, 0) is not int secret || secret == 0;
        }
    }
}
