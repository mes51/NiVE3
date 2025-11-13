using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Dock;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.View.Resource;
using NiVE3.Model.UI;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right1Top, Size = 100)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class AudioInformationViewModel : SingletonePaneViewModelBase
    {
        [ReactiveProperty]
        [NeedWire(nameof(AudioInformationModel), IsOneWay = true)]
        public partial double LeftAudioLevel { get; set; } = double.NegativeInfinity;

        [ReactiveProperty]
        [NeedWire(nameof(AudioInformationModel), IsOneWay = true)]
        public partial double RightAudioLevel { get; set; } = double.NegativeInfinity;

        AudioInformationModel AudioInformationModel { get; }

        public AudioInformationViewModel(AudioInformationModel audioInformationModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.AudioInformationView_Title);
            AudioInformationModel = audioInformationModel;

            WiringModel();
        }

        partial void WiringModel();
    }
}
