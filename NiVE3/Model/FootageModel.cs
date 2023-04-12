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

        private int width;
        public int Width
        {
            get { return width; }
            private set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            private set { SetProperty(ref height, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public string FileName => Path.GetFileName(Input.FilePath);

        IInput Input { get; }

        public FootageModel(IInput input)
        {
            Input = input;
            Name = Path.GetFileName(input.FilePath);
            Width = input.Width;
            Height = input.Height;
            FrameRate = input.FrameRate;
            Duration = input.Duration;
            FilePath = input.FilePath;
        }
    }
}
