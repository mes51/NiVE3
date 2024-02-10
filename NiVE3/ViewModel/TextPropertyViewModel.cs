using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using NiVE3.Model;
using NiVE3.Shared.Extension;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.Text;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Mvvm;
using SixLabors.ImageSharp.Drawing;
using FontFamily = SixLabors.Fonts.FontFamily;
using TextOptions = SixLabors.Fonts.TextOptions;
using FontStyle = SixLabors.Fonts.FontStyle;
using NiVE3.Mvvm;
using System.Windows.Input;
using Prism.Commands;
using NiVE3.Input.Special;
using NiVE3.Property.Types;
using System.Numerics;
using NiVE3.Data;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right1Center)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(Composition), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    partial class TextPropertyViewModel : SingletonePaneViewModelBase
    {
        private int selectedFontGroupIndex;
        public int SelectedFontGroupIndex
        {
            get { return selectedFontGroupIndex; }
            set { SetProperty(ref selectedFontGroupIndex, value); }
        }

        private int selectedFontSubFamilyIndex;
        public int SelectedFontSubFamilyIndex
        {
            get { return selectedFontSubFamilyIndex; }
            set { SetProperty(ref selectedFontSubFamilyIndex, value); }
        }

        private double fontSize = 20.0;
        [NeedWire(nameof(TextPropertyModel))]
        public double FontSize
        {
            get { return fontSize; }
            set { SetProperty(ref fontSize, value); }
        }

        private double lineHeight = 1.0;
        [NeedWire(nameof(TextPropertyModel))]
        public double LineHeight
        {
            get { return lineHeight; }
            set { SetProperty(ref lineHeight, value); }
        }

        private double verticalScale = 100.0;
        [NeedWire(nameof(TextPropertyModel))]
        public double VerticalScale
        {
            get { return verticalScale; }
            set { SetProperty(ref verticalScale, value); }
        }

        private double horizontalScale = 100.0;
        [NeedWire(nameof(TextPropertyModel))]
        public double HorizontalScale
        {
            get { return horizontalScale; }
            set { SetProperty(ref horizontalScale, value); }
        }

        private double letterSpacing;
        [NeedWire(nameof(TextPropertyModel))]
        public double LetterSpacing
        {
            get { return letterSpacing; }
            set { SetProperty(ref letterSpacing, value); }
        }

        private double textLineWidth;
        [NeedWire(nameof(TextPropertyModel))]
        public double TextLineWidth
        {
            get { return textLineWidth; }
            set { SetProperty(ref textLineWidth, value); }
        }

        private TextLineDrawOrder textLineDrawOrder;
        [NeedWire(nameof(TextPropertyModel))]
        public TextLineDrawOrder TextLineDrawOrder
        {
            get { return textLineDrawOrder; }
            set { SetProperty(ref textLineDrawOrder, value); }
        }

        private bool isEnableBold;
        [NeedWire(nameof(TextPropertyModel))]
        public bool IsEnableBold
        {
            get { return isEnableBold; }
            set { SetProperty(ref isEnableBold, value); }
        }

        private bool isEnableItalic;
        [NeedWire(nameof(TextPropertyModel))]
        public bool IsEnableItalic
        {
            get { return isEnableItalic; }
            set { SetProperty(ref isEnableItalic, value); }
        }

        private TextAlign textAlign;
        [NeedWire(nameof(TextPropertyModel))]
        public TextAlign TextAlign
        {
            get { return textAlign; }
            set { SetProperty(ref textAlign, value); }
        }

        private FloatColor fillColor;
        [NeedWire(nameof(TextPropertyModel))]
        public FloatColor FillColor
        {
            get { return fillColor; }
            set { SetProperty(ref fillColor, value); }
        }

        private FloatColor outlineColor;
        [NeedWire(nameof(TextPropertyModel))]
        public FloatColor OutlineColor
        {
            get { return outlineColor; }
            set { SetProperty(ref outlineColor, value); }
        }

        private Guid? currentEditingCompositionId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private Guid? lastSelectedLayerId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? LastSelectedLayerId
        {
            get { return lastSelectedLayerId; }
            set { SetProperty(ref lastSelectedLayerId, value); }
        }

        private double currentTime;
        [ManualWire(nameof(Composition), IsOneWay = true)]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private SolidColorBrush fillColorBrush = Brushes.White;
        public SolidColorBrush FillColorBrush
        {
            get { return fillColorBrush; }
            set { SetProperty(ref fillColorBrush, value); }
        }

        private SolidColorBrush outlineColorBrush = Brushes.Red;
        public SolidColorBrush OutlineColorBrush
        {
            get { return outlineColorBrush; }
            set { SetProperty(ref outlineColorBrush, value); }
        }

        private CompositionModel? compositionModel;
        public CompositionModel? Composition
        {
            get { return compositionModel; }
            set
            {
                if (compositionModel == value)
                {
                    return;
                }

                if (compositionModel != null)
                {
                    UnbindComposition();
                }
                SetProperty(ref compositionModel, value);
                if (value != null)
                {
                    BindComposition();
                }
            }
        }

        LayerModel? TargetLayer { get; set; }

        PropertyModel? SourceTextPropertyModel { get; set; }

        public FontGroupViewModel SelectedFontGroup => Fonts[SelectedFontGroupIndex];

        public bool IsSupportBold => SelectedFontGroup.SubFamiles.ElementAtOrDefault(selectedFontSubFamilyIndex)?.IsSupportBold ?? false;

        public bool IsSupportItalic => SelectedFontGroup.SubFamiles.ElementAtOrDefault(selectedFontSubFamilyIndex)?.IsSupportItalic ?? false;

        public List<FontGroupViewModel> Fonts { get; set; } = new List<FontGroupViewModel>();

        TextPropertyModel TextPropertyModel { get; }

        ProjectModel ProjectModel { get; }

        ViewStateModel ViewState { get; }

        bool IsFontChanging { get; set; }

        bool IsPropertyEditing { get; set; }

        public ICommand BeginEditCommand { get; }

        public ICommand EndEditCommand { get; }

        public ICommand AbortEditCommand { get; }

        object? PrevValue { get; set; }

        public TextPropertyViewModel(TextPropertyModel textPropertyModel, ProjectModel projectModel, ViewStateModel viewStateModel)
        {
            TextPropertyModel = textPropertyModel;
            ProjectModel = projectModel;
            ViewState = viewStateModel;
            Fonts.AddRange(textPropertyModel.FontGroups.Select(f => new FontGroupViewModel(f)));

            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.TextPropertyView_Title);

            WiringModel();

            UpdateSelectedFontFromModel();

            PropertyChanged += TextPropertyViewModel_PropertyChanged;
            textPropertyModel.PropertyChanged += TextPropertyModel_PropertyChanged;

            BeginEditCommand = new DelegateCommand(() =>
            {
                IsPropertyEditing = true;
                if (SourceTextPropertyModel != null)
                {
                    SourceTextPropertyModel.UseEditingValue = true;
                    PrevValue = SourceTextPropertyModel.Value;
                }
            });

            EndEditCommand = new DelegateCommand(() =>
            {
                IsPropertyEditing = false;
                if (SourceTextPropertyModel != null && TargetLayer != null)
                {
                    SourceTextPropertyModel.UseEditingValue = false;
                    var newStyle = TextPropertyModel.GetStyle();
                    SourceTextPropertyModel.CurrentTime = CurrentTime;
                    SourceTextPropertyModel.SourceStartPoint = TargetLayer.SourceStartPoint;
                    SourceTextPropertyModel.CommitProperty(SourceTextPropertyType.ReplaceDefaultStyle(PrevValue, newStyle), PrevValue);
                }
            });

            AbortEditCommand = new DelegateCommand(() =>
            {
                IsPropertyEditing = false;
                if (SourceTextPropertyModel != null)
                {
                    SourceTextPropertyModel.UseEditingValue = false;
                    SourceTextPropertyModel.Value = PrevValue;
                }
            });
        }

        partial void WiringModel();

        partial void BindComposition();

        partial void UnbindComposition();

        void UpdateSelectedFontFromModel()
        {
            if (IsFontChanging)
            {
                return;
            }

            IsFontChanging = true;

            var newSelectedFont = TextPropertyModel.SelectedFont;
            var groupIndex = Fonts.FindIndex(f => f.SubFamiles.Any(sf => sf.FontInfo.UniqueId == newSelectedFont.UniqueId));
            var subFamilyIndex = Fonts[groupIndex].SubFamiles.IndexOf(sf => sf.FontInfo.UniqueId == newSelectedFont.UniqueId);
            SelectedFontGroupIndex = groupIndex;
            SelectedFontSubFamilyIndex = subFamilyIndex;

            IsFontChanging = false;
        }

        void UpdateTargetLayer()
        {
            TargetLayer = Composition?.Layers?.FirstOrDefault(l => l.LayerId == LastSelectedLayerId);
            SourceTextPropertyModel = TargetLayer?.TextProperties?.FindProperty(TextFootageSource.SourceTextId) as PropertyModel;
            UpdateTextPropertyFromLayer();
        }

        void UpdateTextPropertyFromLayer()
        {
            if (SourceTextPropertyModel == null || TargetLayer == null)
            {
                return;
            }

            IsFontChanging = true;

            var style = SourceTextPropertyType.GetDefaultStyle(SourceTextPropertyModel.GetValue(CurrentTime - TargetLayer.SourceStartPoint));
            if (style != null)
            {
                TextPropertyModel.SetStyle(style);
            }

            IsFontChanging = false;

            UpdateSelectedFontFromModel();
        }

        void ChangeTextLayerProperty()
        {
            if (IsFontChanging || SourceTextPropertyModel == null || TargetLayer == null)
            {
                return;
            }

            var newStyle = TextPropertyModel.GetStyle();
            if (IsPropertyEditing)
            {
                SourceTextPropertyModel.Value = SourceTextPropertyType.ReplaceDefaultStyle(PrevValue, newStyle);
            }
            else
            {
                var prevValue = SourceTextPropertyModel.Value;
                SourceTextPropertyModel.CurrentTime = CurrentTime;
                SourceTextPropertyModel.SourceStartPoint = TargetLayer.SourceStartPoint;
                SourceTextPropertyModel.CommitProperty(SourceTextPropertyType.ReplaceDefaultStyle(prevValue, newStyle), prevValue);
            }
        }

        private void TextPropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextPropertyModel.SelectedFont))
            {
                UpdateSelectedFontFromModel();
            }
        }

        private void TextPropertyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedFontGroupIndex) when !IsFontChanging:
                    RaisePropertyChanged(nameof(SelectedFontGroup));
                    SelectedFontSubFamilyIndex = 0;
                    break;
                case nameof(SelectedFontSubFamilyIndex) when !IsFontChanging:
                    if (SelectedFontSubFamilyIndex > -1)
                    {
                        IsFontChanging = true;
                        TextPropertyModel.SelectedFont = SelectedFontGroup.SubFamiles[SelectedFontSubFamilyIndex].FontInfo;
                        IsFontChanging = false;
                    }
                    ChangeTextLayerProperty();
                    RaisePropertyChanged(nameof(IsSupportBold));
                    RaisePropertyChanged(nameof(IsSupportItalic));
                    break;
                case nameof(CurrentEditingCompositionId):
                    if (CurrentEditingCompositionId != Composition?.CompositionId)
                    {
                        Composition = ProjectModel.CompositionModels.FirstOrDefault(c => c.CompositionId == CurrentEditingCompositionId);
                        UpdateTargetLayer();
                    }
                    break;
                case nameof(LastSelectedLayerId):
                    UpdateTargetLayer();
                    break;
                case nameof(FontSize):
                case nameof(LineHeight):
                case nameof(VerticalScale):
                case nameof(HorizontalScale):
                case nameof(LetterSpacing):
                case nameof(TextLineWidth):
                case nameof(TextLineDrawOrder):
                case nameof(IsEnableBold):
                case nameof(IsEnableItalic):
                case nameof(TextAlign):
                    ChangeTextLayerProperty();
                    break;
                case nameof(FillColor):
                    ChangeTextLayerProperty();
                    FillColorBrush = new SolidColorBrush(FillColor.ToByteColor());
                    break;
                case nameof(OutlineColor):
                    OutlineColorBrush = new SolidColorBrush(OutlineColor.ToByteColor());
                    ChangeTextLayerProperty();
                    break;
                case nameof(CurrentTime):
                    UpdateTextPropertyFromLayer();
                    break;
            }
        }
    }

    class FontGroupViewModel : BindableBase
    {
        static readonly object LockObject = new object();

        public FontGroup FontFamily { get; }

        public FontSubFamilyViewModel[] SubFamiles { get; }

        public string FontName => FontFamily.FontName;

        private GeometryCollection? sample;
        public GeometryCollection? Sample
        {
            get
            {
                if (sample == null)
                {
                    CreateSampleGeometry();
                }
                return sample;
            }
            private set { SetProperty(ref sample, value); }
        }

        public FontGroupViewModel(FontGroup fontFamily)
        {
            FontFamily = fontFamily;
            SubFamiles = fontFamily.SubFamiles.Select(kv => new FontSubFamilyViewModel(kv.Value, kv.Key)).ToArray();
        }

        void CreateSampleGeometry()
        {
            const float FontSize = 20.0F;

            Task.Factory.StartNew(() =>
            {
                if (sample != null)
                {
                    return;
                }

                var collection = new GeometryCollection();
                lock (LockObject)
                {
                    var font = FontFamily.FontInfos[0].FontFamily;
                    var paths = TextBuilder.GenerateGlyphs("参麩靇 サンプル Sample", new TextOptions(font.CreateFont(FontSize)));
                    foreach (var path in paths)
                    {
                        foreach (var simplePath in path.Flatten())
                        {
                            var geometry = new StreamGeometry();
                            geometry.FillRule = FillRule.Nonzero;
                            using (var context = geometry.Open())
                            {
                                var span = simplePath.Points.Span;
                                context.BeginFigure(new System.Windows.Point(span[0].X, span[0].Y), true, simplePath.IsClosed);
                                for (var i = 1; i < span.Length; i++)
                                {
                                    context.LineTo(new System.Windows.Point(span[i].X, span[i].Y), false, false);
                                }
                            }
                            collection.Add(geometry);
                        }
                    }

                    collection.Freeze();
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Sample = collection;
                }), DispatcherPriority.ApplicationIdle);
            });
        }
    }

    class FontSubFamilyViewModel : BindableBase
    {
        static readonly object LockObject = new object();

        public FontInfo FontInfo { get; }

        public bool IsSupportBold { get; }

        public bool IsSupportItalic { get; }

        public string SubFamilyName { get; }

        private GeometryCollection? sample;
        public GeometryCollection? Sample
        {
            get
            {
                if (sample == null)
                {
                    CreateSampleGeometry();
                }
                return sample;
            }
            private set { SetProperty(ref sample, value); }
        }

        public FontSubFamilyViewModel(FontInfo fontInfo, string subFamilyName)
        {
            FontInfo = fontInfo;
            IsSupportBold = fontInfo.FontFamily.GetAvailableStyles().Contains(FontStyle.Bold);
            IsSupportItalic = fontInfo.FontFamily.GetAvailableStyles().Contains(FontStyle.Italic);
            SubFamilyName = subFamilyName;
        }

        void CreateSampleGeometry()
        {
            const float FontSize = 20.0F;

            Task.Factory.StartNew(() =>
            {
                if (sample != null)
                {
                    return;
                }

                var collection = new GeometryCollection();
                lock (LockObject)
                {
                    var font = FontInfo.FontFamily;
                    var paths = TextBuilder.GenerateGlyphs("参麩靇 サンプル Sample", new TextOptions(font.CreateFont(FontSize)));
                    foreach (var path in paths)
                    {
                        foreach (var simplePath in path.Flatten())
                        {
                            var geometry = new StreamGeometry();
                            geometry.FillRule = FillRule.Nonzero;
                            using (var context = geometry.Open())
                            {
                                var span = simplePath.Points.Span;
                                context.BeginFigure(new System.Windows.Point(span[0].X, span[0].Y), true, simplePath.IsClosed);
                                for (var i = 1; i < span.Length; i++)
                                {
                                    context.LineTo(new System.Windows.Point(span[i].X, span[i].Y), false, false);
                                }
                            }
                            collection.Add(geometry);
                        }
                    }
                    collection.Freeze();
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Sample = collection;
                }), DispatcherPriority.ApplicationIdle);
            });
        }
    }
}
