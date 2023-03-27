using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class FootageModel : BindableBase
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string FileName => Path.GetFileName(Input.FilePath);

        IInput Input { get; }

        public FootageModel(IInput input)
        {
            Input = input;
            Name = Path.GetFileName(input.FilePath);
        }
    }
}
