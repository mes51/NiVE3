using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class InputModel : BindableBase, IDisposable
    {
        public Guid InputId { get; } = Guid.NewGuid();

        public IInput Input { get; }

        public string FilePath => Input.FilePath;

        ExportLifetimeContext<IInput>? InputContext { get; }

        public InputModel(IInput input)
        {
            Input = input;
        }

        public InputModel(ExportLifetimeContext<IInput> inputContext) : this(inputContext.Value)
        {
            InputContext = inputContext;
        }

        public void Dispose()
        {
            Input.Dispose();
            InputContext?.Dispose();
        }
    }
}
