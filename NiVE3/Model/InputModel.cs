using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Extension;
using NiVE3.Input.Special;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class InputModel : BindableBase, IDisposable
    {
        public Guid InputId { get; }

        public IInput Input { get; }

        public Guid PluginId { get; }

        public bool IsSupportLoadToGpu { get; }

        public bool IsSpecial { get; }

        public string FilePath => Input.FilePath;

        public bool IsPlaceholder => Input is PlaceholderInput;

        ExportLifetimeContext<IInput>? InputContext { get; }

        public InputModel(IInput input, Guid pluginId, bool isSupportLoadToGpu, Guid? inputId = null)
        {
            Input = input;
            PluginId = pluginId;
            IsSupportLoadToGpu = isSupportLoadToGpu;
            IsSpecial = input.IsApplied<SpecialInputAttribute>();
            InputId = inputId ?? Guid.NewGuid();
        }

        public InputModel(ExportLifetimeContext<IInput> inputContext, Guid pluginId, bool isSupportLoadToGpu, Guid? inputId = null) : this(inputContext.Value, pluginId, isSupportLoadToGpu, inputId)
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
                InputOption = Input.SaveData(),
                Sources = Input.GetGroup().Flatten().Select(s => new SourceData { SourceId = s.SourceId, SourceType = s.SourceType, Width = s.Width, Height = s.Height, Duration = s.Duration, FrameRate = s.FrameRate }).ToArray()
            };
        }

        public void Dispose()
        {
            InputContext?.Dispose();
        }
    }
}
