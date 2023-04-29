using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    interface IFootageViewModel : INotifyPropertyChanged, IFootageViewModelList
    {
        Guid FootageId { get; }

        string Name { get; set; }

        int Width { get; }

        int Height { get; }

        double FrameRate { get; }

        double Duration { get; }

        string FilePath { get; }

        string Comment { get; set; }

        string? EditingPropertyName { get; }

        bool IsFolder { get; }

        void BeginEditProperty(string propertyName);

        void EndEditProperty();
    }

    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

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

        private string fileExtension;
        public string FileExtension
        {
            get { return fileExtension; }
            set { SetProperty(ref fileExtension, value); }
        }

        private string comment = "";
        [NeedWire(nameof(Footage))]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private string? editingPropertyName;
        public string? EditingPropertyName
        {
            get { return editingPropertyName; }
            private set { SetProperty(ref editingPropertyName, value); }
        }

        public bool IsFolder => false;

        public ObservableCollectionView<IFootageModel, IFootageViewModel>? Footages => null;

        FootageModel Footage { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageViewModel(FootageModel footage)
#pragma warning restore CS8618
        {
            Footage = footage;
            FootageId = footage.FootageId;
            Name = footage.Name;
            Width = footage.Width;
            Height = footage.Height;
            FrameRate = footage.FrameRate;
            Duration = footage.Duration;
            FilePath = footage.FilePath;
            FileExtension = Path.GetExtension(footage.FilePath);
            Comment = footage.Comment;

            WiringModel();

            PropertyChanged += FootageViewModel_PropertyChanged;
        }

        private void FootageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilePath))
            {
                FileExtension = Path.GetExtension(FilePath);
            }
        }

        partial void WiringModel();

        public void BeginEditProperty(string propertyName)
        {
            EditingPropertyName = propertyName;
        }

        public void EndEditProperty()
        {
            // TODO: ヒストリの確定

            EditingPropertyName = null;
        }
    }

    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageFolderViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

        private string name;
        [NeedWire(nameof(Folder))]
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

        private string comment;
        [NeedWire(nameof(Folder))]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private ObservableCollectionView<IFootageModel, IFootageViewModel> footages;
        public ObservableCollectionView<IFootageModel, IFootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        private string? editingPropertyName;
        public string? EditingPropertyName
        {
            get { return editingPropertyName; }
            private set { SetProperty(ref editingPropertyName, value); }
        }

        public bool IsFolder => true;

        FootageFolderModel Folder { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageFolderViewModel(FootageFolderModel folder)
#pragma warning restore CS8618
        {
            Folder = folder;
            FootageId = folder.FootageId;
            Name = folder.Name;
            Comment = folder.Comment;
            Footages = folder.Children.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel ? new FootageViewModel((FootageModel)m) : new FootageFolderViewModel((FootageFolderModel)m));
        }

        partial void WiringModel();

        public void BeginEditProperty(string propertyName)
        {
            EditingPropertyName = propertyName;
        }

        public void EndEditProperty()
        {
            // TODO: ヒストリの確定

            EditingPropertyName = null;
        }
    }
}