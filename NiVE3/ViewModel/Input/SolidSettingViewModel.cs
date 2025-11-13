using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Data;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Input
{
    [UseReactiveProperty]
    partial class SolidSettingViewModel : BindableBase
    {
        const int MinSize = 4;

        [ReactiveProperty]
        public partial string Name { get; set; } = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.SolidInputSettingView_DefaultName);

        [ReactiveProperty]
        public partial int Width { get; set; } = 1920;

        [ReactiveProperty]
        public partial int Height { get; set; } = 1080;

        [ReactiveProperty]
        public partial bool IsFixRatio { get; set; }

        [ReactiveProperty]
        public partial FloatColor Color { get; set; } = FloatColor.FromColor(Colors.White);

        [ReactiveProperty]
        public partial Brush ColorBrush { get; set; } = Brushes.White;

        [ReactiveProperty]
        public partial Int32Size? CompositionSize { get; set; }

        public double FixedRatio { get; private set; }

        public ICommand ChangeToCompositionSizeCommand { get; }

        public SolidSettingViewModel(Int32Size? compositionSize)
        {
            CompositionSize = compositionSize;

            ChangeToCompositionSizeCommand = new DelegateCommand(() =>
            {
                var compSize = CompositionSize ?? Int32Size.Empty;
                Width = compSize.Width;
                Height = compSize.Height;
            }, () => CompositionSize != null).ObservesProperty(() => CompositionSize);

            PropertyChanged += SolidSettingViewModel_PropertyChanged;
        }

        private void SolidSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Width):
                    if (IsFixRatio)
                    {
                        Height = (int)(Width / FixedRatio);
                    }
                    break;
                case nameof(Height):
                    if (IsFixRatio)
                    {
                        Width = (int)(Height * FixedRatio);
                    }
                    break;
                case nameof(IsFixRatio):
                    if (IsFixRatio)
                    {
                        FixedRatio = Width / (double)Height;
                    }
                    break;
                case nameof(Color):
                    ColorBrush = new SolidColorBrush(Color.ToByteColor());
                    break;
            }
        }
    }
}
