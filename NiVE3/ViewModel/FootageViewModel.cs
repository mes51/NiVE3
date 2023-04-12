using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageViewModel : BindableBase, IEditableObject
    {
        private string name;
        [NeedWire(nameof(Footage))]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double frameRate;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double duration;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private string filePath;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private string fileType;
        public string FileType
        {
            get { return fileType; }
            set { SetProperty(ref fileType, value); }
        }

        private string comment = "";
        [NeedWire(nameof(Footage))]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        FootageModel Footage { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageViewModel(FootageModel footage)
#pragma warning restore CS8618
        {
            Footage = footage;
            Name = footage.Name;
            Width = footage.Width;
            Height = footage.Height;
            FrameRate = footage.FrameRate;
            Duration = footage.Duration;
            FilePath = footage.FilePath;
            FileType = Path.GetExtension(footage.FilePath);

            WiringModel();

            PropertyChanged += FootageViewModel_PropertyChanged;
        }

        private void FootageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilePath))
            {
                FileType = Path.GetExtension(FilePath);
            }
        }

        partial void WiringModel();

        public void BeginEdit()
        {
        }

        public void CancelEdit()
        {
        }

        public void EndEdit()
        {
        }
    }
}