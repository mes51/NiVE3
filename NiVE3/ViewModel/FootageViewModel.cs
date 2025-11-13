using System;
using System.Buffers;
using System.Collections.Generic;
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
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Mvvm;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel
{
    interface IFootageViewModel : INotifyPropertyChanged, IFootageViewModelList
    {
        Guid FootageId { get; }

        string Name { get; set; }

        int Width { get; }

        int Height { get; }

        double FrameRate { get; }

        Time Duration { get; }

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

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial int Width { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial int Height { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial string FilePath { get; set; } = "";

        [ReactiveProperty]
        public partial string FileExtension { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial string Comment { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(Footage), IsOneWay = true)]
        public partial SourceType InputType { get; set; }

        public bool IsFolder => false;

        public BitmapSource? SampleImage
        {
            get
            {
                if (IsDirty)
                {
                    UpdateSampleImage();
                }
                return field;
            }
            set { SetProperty(ref field, value); }
        }

        [ReactiveProperty]
        public partial EditingFootageParameter EditingParameter { get; set; }

        public ObservableCollectionView<IFootageModel, IFootageViewModel>? Footages => null;

        public bool HasImage => InputType.HasFlag(SourceType.Image) || InputType.HasFlag(SourceType.Video);

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        FootageModel Footage { get; }

        ApplicationModel ApplicationModel { get; }

        bool IsDirty { get; set; } = true;

        public FootageViewModel(FootageModel footage, ApplicationModel applicationModel)
        {
            Footage = footage;
            ApplicationModel = applicationModel;
            FootageId = footage.FootageId;
            Name = footage.Name;
            Width = footage.Width;
            Height = footage.Height;
            FrameRate = footage.FrameRate;
            Duration = footage.Duration;
            FilePath = footage.FilePath;
            Comment = footage.Comment;
            InputType = footage.InputType;
            if (!footage.InputModel.IsInternalInput)
            {
                FileExtension = Path.GetExtension(footage.FilePath);
            }

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

            PropertyChanged += FootageViewModel_PropertyChanged;
            footage.UpdateSampleImageRequest += Footage_UpdateSampleImageRequest;
        }

        private void FootageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FilePath):
                    FileExtension = Path.GetExtension(FilePath);
                    IsDirty = true;
                    break;
                case nameof(InputType):
                    RaisePropertyChanged(nameof(HasImage));
                    break;
            }
        }

        partial void WiringModel();

        void UpdateSampleImage()
        {
            IsDirty = false;
            if (Footage.InputType == SourceType.Image || (Footage.InputType & SourceType.Video) != SourceType.None)
            {
                using var checker = CycleChecker.StartCheck();
                using var image = Footage.ReadImage(Duration * 0.5, 1.0, 0, 0, null, null, ImageInterpolationQuality.Level2, ApplicationModel.UseGpu);
                if (image != null)
                {
                    var data = ArrayPool<byte>.Shared.Rent(image.DataLength * 4);
                    ImageConversion.ConvertToBGRA32(image.GetData()[..image.DataLength], data, image.DataLength);
                    var writable = new WriteableBitmap(image.Width, image.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                    writable.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), data, image.Width * 4, 0);
                    SampleImage = writable;
                    ArrayPool<byte>.Shared.Return(data);
                }
            }
        }

        private void Footage_UpdateSampleImageRequest(object? sender, EventArgs e)
        {
            IsDirty = true;
        }
    }

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel))]
    partial class FootageFolderViewModel : BindableBase, IFootageViewModel
    {
        public Guid FootageId { get; private set; }

        [ReactiveProperty]
        [NeedWire(nameof(Folder), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        public int Width => 0;

        public int Height => 0;

        public double FrameRate => 0.0;

        public Time Duration => Time.Zero;

        public string FilePath => "";

        [ReactiveProperty]
        [NeedWire(nameof(Folder), IsOneWay = true)]
        public partial string Comment { get; set; } = "";

        public SourceType InputType => SourceType.None;

        [ReactiveProperty]
        public partial ObservableCollectionView<IFootageModel, IFootageViewModel> Footages { get; set; }

        public bool IsFolder => true;

        public BitmapSource? SampleImage => null;

        [ReactiveProperty]
        public partial EditingFootageParameter EditingParameter { get; set; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        FootageFolderModel Folder { get; }

        public FootageFolderViewModel(FootageFolderModel folder, ApplicationModel applicationModel)
        {
            Folder = folder;
            FootageId = folder.FootageId;
            Name = folder.Name;
            Comment = folder.Comment;
            Footages = folder.Children.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel model ? new FootageViewModel(model, applicationModel) : new FootageFolderViewModel((FootageFolderModel)m, applicationModel));

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