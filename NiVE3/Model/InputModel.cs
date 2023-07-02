using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class InputModel : BindableBase
    {
        public Guid InputId { get; } = Guid.NewGuid();

        public IInput Input { get; }

        public string FilePath => Input.FilePath;

        public InputModel(IInput input)
        {
            Input = input;
        }
    }
}
