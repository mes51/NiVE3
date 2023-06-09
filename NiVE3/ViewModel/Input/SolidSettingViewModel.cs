using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Data;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Input
{
    class SolidSettingViewModel : BindableBase
    {
        const int MinSize = 4;

        private string name = "平面";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width = 1920;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, Math.Max(value, MinSize)); }
        }

        private int height = 1080;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, Math.Max(value, MinSize)); }
        }

        private bool isFixRatio;
        public bool IsFixRatio
        {
            get { return isFixRatio; }
            set { SetProperty(ref isFixRatio, value); }
        }

        private FloatColor color = FloatColor.FromColor(Colors.White);
        public FloatColor Color
        {
            get { return color; }
            set { SetProperty(ref color, value); }
        }

        private Brush colorBrush = Brushes.White;
        public Brush ColorBrush
        {
            get { return colorBrush; }
            set { SetProperty(ref colorBrush, value); }
        }

        public double FixedRatio { get; private set; }

        public SolidSettingViewModel()
        {
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
