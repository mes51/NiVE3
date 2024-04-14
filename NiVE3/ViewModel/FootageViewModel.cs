using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
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

        SourceType InputType { get; }

        bool IsFolder { get; }

        BitmapSource? SampleImage { get; }

        EditingFootageParameter EditingParameter { get; set; }

        ICommand BeginEditNameCommand { get; }

        ICommand EndEditNameCommand { get; }

        ICommand BeginEditCommentCommand { get; }

        ICommand EndEditCommentCommand { get; }
    }

    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

        private string name;
        [NeedWire(nameof(Footage), IsOneWay = true)]
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
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private SourceType inputType;
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public SourceType InputType
        {
            get { return inputType; }
            set { SetProperty(ref inputType, value); }
        }

        public bool IsFolder => false;

        private BitmapSource? sampleImage;
        public BitmapSource? SampleImage
        {
            get { return sampleImage; }
            set { SetProperty(ref sampleImage, value); }
        }

        private EditingFootageParameter editingParameter;
        public EditingFootageParameter EditingParameter
        {
            get { return editingParameter; }
            set { SetProperty(ref editingParameter, value); }
        }

        public ObservableCollectionView<IFootageModel, IFootageViewModel>? Footages => null;

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

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
            InputType = footage.InputType;

            UpdateSampleImage();

            WiringModel();

            PropertyChanged += FootageViewModel_PropertyChanged;

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                PrevName = Name;
                EditingParameter = EditingFootageParameter.Name;
            });

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit && !string.IsNullOrEmpty(Name))
                {
                    Footage.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                EditingParameter = EditingFootageParameter.None;
            });

            BeginEditCommentCommand = new DelegateCommand(() =>
            {
                PrevComment = Comment;
                EditingParameter = EditingFootageParameter.Comment;
            });

            EndEditCommentCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit)
                {
                    Footage.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                EditingParameter = EditingFootageParameter.None;
            });
        }

        private void FootageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilePath))
            {
                FileExtension = Path.GetExtension(FilePath);
                UpdateSampleImage();
            }
        }

        partial void WiringModel();

        void UpdateSampleImage()
        {
            if (Footage.InputType == SourceType.Image || (Footage.InputType & SourceType.Video) != SourceType.None)
            {
                using var image = Footage.ReadImage(Duration * 0.5, 1.0, 0, 0, null, ImageInterpolationQuality.Level2, false) as NManagedImage;
                if (image != null)
                {
                    var data = ArrayPool<byte>.Shared.Rent(image.DataLength * 4);
                    ImageConversion.ConvertToBGRA32(image.GetDataSpan(), data, image.DataLength);
                    var writable = new WriteableBitmap(image.Width, image.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                    writable.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), data, image.Width * 4, 0);
                    SampleImage = writable;
                    ArrayPool<byte>.Shared.Return(data);
                }
            }
        }
    }

    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageFolderViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

        private string name;
        [NeedWire(nameof(Folder), IsOneWay = true)]
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
        [NeedWire(nameof(Folder), IsOneWay = true)]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public SourceType InputType => SourceType.None;

        private ObservableCollectionView<IFootageModel, IFootageViewModel> footages;
        public ObservableCollectionView<IFootageModel, IFootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        public bool IsFolder => true;

        public BitmapSource? SampleImage => null;

        private EditingFootageParameter editingParameter;
        public EditingFootageParameter EditingParameter
        {
            get { return editingParameter; }
            set { SetProperty(ref editingParameter, value); }
        }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        FootageFolderModel Folder { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageFolderViewModel(FootageFolderModel folder)
#pragma warning restore CS8618
        {
            Folder = folder;
            FootageId = folder.FootageId;
            Name = folder.Name;
            Comment = folder.Comment;
            Footages = folder.Children.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel model ? new FootageViewModel(model) : new FootageFolderViewModel((FootageFolderModel)m));

            WiringModel();

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                PrevName = Name;
                EditingParameter = EditingFootageParameter.Name;
            });

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit && !string.IsNullOrEmpty(Name))
                {
                    Folder.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                EditingParameter = EditingFootageParameter.None;
            });

            BeginEditCommentCommand = new DelegateCommand(() =>
            {
                PrevComment = Comment;
                EditingParameter = EditingFootageParameter.Comment;
            });

            EndEditCommentCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (commit)
                {
                    Folder.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                EditingParameter = EditingFootageParameter.None;
            });
        }

        partial void WiringModel();
    }
}