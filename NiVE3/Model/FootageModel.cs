using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    interface IFootageModel : INotifyPropertyChanged
    {
        Guid FootageId { get; }

        string Name { get; set; }

        int Width { get; }

        int Height { get; }

        double FrameRate { get; }

        double Duration { get; }

        string FilePath { get; }

        string Comment { get; set; }

        string FileName { get; }

        ObservableCollection<IFootageModel>? Children { get; }
    }

    class FootageModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; } = Guid.NewGuid();

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
            private set { SetProperty(ref frameRate, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            private set { SetProperty(ref duration, value); }
        }

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            private set { SetProperty(ref filePath, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public string FileName => Path.GetFileName(Input.FilePath);

        public ObservableCollection<IFootageModel>? Children => null;

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

    class FootageFolderModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; } = Guid.NewGuid();

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public int Width => 0;

        public int Height => 0;

        public double FrameRate => 0.0;

        public double Duration => 0.0;

        public string FilePath => "";

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public string FileName => "";

        private ObservableCollection<IFootageModel> children = new ObservableCollection<IFootageModel>();
        public ObservableCollection<IFootageModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public FootageFolderModel()
        {
            FootageId = Guid.NewGuid();
            Name = "New Folder";
        }
    }
}
