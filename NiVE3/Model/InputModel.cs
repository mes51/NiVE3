using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Project;
using NiVE3.Extension;
using NiVE3.Input.Special;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class InputModel : BindableBase, IDisposable
    {
        public Guid InputId { get; } = Guid.NewGuid();

        public IInput Input { get; }

        public Guid PluginId { get; }

        public bool IsSupportLoadToGpu { get; }

        public bool IsSpecial { get; }

        public string FilePath => Input.FilePath;

        ExportLifetimeContext<IInput>? InputContext { get; }

        public InputModel(IInput input, Guid pluginId, bool isSupportLoadToGpu)
        {
            Input = input;
            PluginId = pluginId;
            IsSupportLoadToGpu = isSupportLoadToGpu;
            IsSpecial = input.IsApplied<SpecialInputAttribute>();
        }

        public InputModel(ExportLifetimeContext<IInput> inputContext, Guid pluginId, bool isSupportLoadToGpu) : this(inputContext.Value, pluginId, isSupportLoadToGpu)
        {
            InputContext = inputContext;
        }

        public InputData SaveData()
        {
            return new InputData
            {
                InputId = InputId,
                PluginId = PluginId,
                FilePath = FilePath,
                InputOption = Input.SaveData()
            };
        }

        public void Dispose()
        {
            Input.Dispose();
            InputContext?.Dispose();
        }
    }
}
