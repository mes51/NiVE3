using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Input;
using NiVE3.PresetPlugin.Internal.Mvvm;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    [UseReactiveProperty]
    partial class DirectShowInputSettingViewModel : BindableBase
    {
        [ReactiveProperty]
        public partial VideoAlphaType VideoAlphaType { get; set; }
    }
}
