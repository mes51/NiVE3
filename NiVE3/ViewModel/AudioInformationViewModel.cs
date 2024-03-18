using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.View.Dock;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Mvvm;
using System.ComponentModel;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right1Top, Size = 100)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class AudioInformationViewModel : SingletonePaneViewModelBase
    {
        private double leftAudioLevel = double.NegativeInfinity;
        [NeedWire(nameof(AudioInformationModel), IsOneWay = true)]
        public double LeftAudioLevel
        {
            get { return leftAudioLevel; }
            set { SetProperty(ref leftAudioLevel, value); }
        }

        private double rightAudioLevel = double.NegativeInfinity;
        [NeedWire(nameof(AudioInformationModel), IsOneWay = true)]
        public double RightAudioLevel
        {
            get { return rightAudioLevel; }
            set { SetProperty(ref rightAudioLevel, value); }
        }

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
