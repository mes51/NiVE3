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
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;
using SixLabors.ImageSharp.Drawing;
using FontFamily = SixLabors.Fonts.FontFamily;
using TextOptions = SixLabors.Fonts.TextOptions;
using FontStyle = SixLabors.Fonts.FontStyle;
using System.Windows.Input;
using Prism.Commands;
using NiVE3.Input;
using NiVE3.Property.Types;
using NiVE3.Data;
using NiVE3.Model.UI;
using NiVE3.Util;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right1Center, Size = 351)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(Composition), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    partial class TextPropertyViewModel : SingletonePaneViewModelBase
    {
        [ReactiveProperty]
        public partial int SelectedFontGroupIndex { get; set; }

        [ReactiveProperty]
        public partial int SelectedFontSubFamilyIndex { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double FontSize { get; set; } = 20.0;

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double LineHeight { get; set; } = 1.0;

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double VerticalScale { get; set; } = 100.0;

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double HorizontalScale { get; set; } = 100.0;

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double LetterSpacing { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial double TextLineWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial TextLineDrawOrder TextLineDrawOrder { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial bool IsEnableBold { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial bool IsEnableItalic { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial TextAlign TextAlign { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial FloatColor FillColor { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(TextPropertyModel))]
        public partial FloatColor TextLineColor { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial Guid? CurrentEditingCompositionId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial Guid? LastSelectedLayerId { get; set; }

        [ReactiveProperty]
        [ManualWire(nameof(Composition), IsOneWay = true)]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        public partial SolidColorBrush FillColorBrush { get; set; } = Brushes.White;

        [ReactiveProperty]
        public partial SolidColorBrush TextLineColorBrush { get; set; } = Brushes.Red;

        public CompositionModel? Composition
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                if (field != null)
                {
                    UnbindComposition();
                }
                SetProperty(ref field, value);
                if (value != null)
                {
                    BindComposition();
                }
            }
        }

        LayerModel? TargetLayer { get; set; }

        PropertyModel? SourceTextPropertyModel { get; set; }

        public FontGroupViewModel SelectedFontGroup => Fonts[SelectedFontGroupIndex];

        public bool IsSupportBold => SelectedFontGroup.SubFamiles.ElementAtOrDefault(SelectedFontSubFamilyIndex)?.IsSupportBold ?? false;

        public bool IsSupportItalic => SelectedFontGroup.SubFamiles.ElementAtOrDefault(SelectedFontSubFamilyIndex)?.IsSupportItalic ?? false;

        public List<FontGroupViewModel> Fonts { get; set; } = [];

        TextPropertyModel TextPropertyModel { get; }

        ProjectModel ProjectModel { get; }

        ViewStateModel ViewState { get; }

        EventHubModel EventHubModel { get; }

        bool IsFontChanging { get; set; }

        bool IsPropertyEditing { get; set; }

        public ICommand BeginEditCommand { get; }

        public ICommand EndEditCommand { get; }

        public ICommand AbortEditCommand { get; }

        public ICommand CreateFontSampleGeometryCommand { get; }

        public ICommand CreateSubFamilySampleGeometryCommaned { get; }

        object? PrevValue { get; set; }

        bool FontSampleCreated { get; set; }

        public TextPropertyViewModel(TextPropertyModel textPropertyModel, ProjectModel projectModel, ViewStateModel viewStateModel, EventHubModel eventHubModel)
        {
            TextPropertyModel = textPropertyModel;
            ProjectModel = projectModel;
            ViewState = viewStateModel;
            EventHubModel = eventHubModel;
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
                    PrevValue = SourceTextPropertyModel.GetRawValue(CurrentTime - SourceTextPropertyModel.SourceStartPoint);
                    SourceTextPropertyModel.UseEditingValue = true;
                }
            });

            EndEditCommand = new DelegateCommand(() =>
            {
                IsPropertyEditing = false;
                if (CurrentEditingCompositionId != null && SourceTextPropertyModel != null && TargetLayer != null)
                {
                    SourceTextPropertyModel.UseEditingValue = false;
                    EventHubModel.NotifyTextStyleChange(CurrentEditingCompositionId.Value, LastSelectedLayerId, PrevValue);
                }
            });

            AbortEditCommand = new DelegateCommand(() =>
            {
                IsPropertyEditing = false;
                if (SourceTextPropertyModel != null)
                {
                    SourceTextPropertyModel.UseEditingValue = false;
                    SourceTextPropertyModel.UpdateUncommitedRawValue(PrevValue);
                }
            });

            CreateFontSampleGeometryCommand = new DelegateCommand(() =>
            {
                if (!FontSampleCreated)
                {
                    FontViewModelBase.CreateSampleGeometry(Fonts);
                    FontSampleCreated = true;
                }
            });

            CreateSubFamilySampleGeometryCommaned = new DelegateCommand(() =>
            {
                if (SelectedFontSubFamilyIndex > -1)
                {
                    FontViewModelBase.CreateSampleGeometry(Fonts[SelectedFontGroupIndex].SubFamiles);
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
            var subFamilyIndex = Fonts[groupIndex].SubFamiles.FindIndex(sf => sf.FontInfo.UniqueId == newSelectedFont.UniqueId);
            SelectedFontGroupIndex = groupIndex;
            RaisePropertyChanged(nameof(SelectedFontGroup));
            SelectedFontSubFamilyIndex = subFamilyIndex;

            IsFontChanging = false;
        }

        void UpdateTargetLayer()
        {
            if (SourceTextPropertyModel != null)
            {
                SourceTextPropertyModel.ValueUpdated -= SourceTextPropertyModel_ValueUpdated;
            }

            TargetLayer = Composition?.Layers?.FirstOrDefault(l => l.LayerId == LastSelectedLayerId);
            SourceTextPropertyModel = TargetLayer?.TextProperties?.FindProperty(TextFootageSource.SourceTextId) as PropertyModel;
            UpdateTextPropertyFromLayer();

            if (SourceTextPropertyModel != null)
            {
                SourceTextPropertyModel.ValueUpdated += SourceTextPropertyModel_ValueUpdated;
            }
        }

        void UpdateTextPropertyFromLayer()
        {
            if (SourceTextPropertyModel == null || TargetLayer == null)
            {
                return;
            }

            IsFontChanging = true;

            var style = SourceTextPropertyType.GetDefaultStyle(SourceTextPropertyModel.GetRawValue(CurrentTime - TargetLayer.SourceStartPoint));
            if (style != null)
            {
                TextPropertyModel.SetStyle(style);
            }

            IsFontChanging = false;

            UpdateSelectedFontFromModel();
        }

        void ChangeTextLayerProperty()
        {
            if (IsFontChanging || SourceTextPropertyModel == null || TargetLayer == null || CurrentEditingCompositionId == null)
            {
                return;
            }

            var newStyle = TextPropertyModel.GetStyle();
            if (IsPropertyEditing)
            {
                SourceTextPropertyModel.UpdateUncommitedRawValue(SourceTextPropertyType.ReplaceDefaultStyle(PrevValue, newStyle));
            }
            else
            {
                EventHubModel.NotifyTextStyleChange(CurrentEditingCompositionId.Value, null, null);
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
                    if (SelectedFontSubFamilyIndex != 0)
                    {
                        SelectedFontSubFamilyIndex = 0;
                    }
                    else
                    {
                        ChangeTextLayerProperty();
                    }
                    break;
                case nameof(SelectedFontSubFamilyIndex) when !IsFontChanging:
                    if (SelectedFontSubFamilyIndex > -1)
                    {
                        IsFontChanging = true;
                        TextPropertyModel.SelectedFont = SelectedFontGroup.SubFamiles[SelectedFontSubFamilyIndex].FontInfo;
                        IsFontChanging = false;
                        ChangeTextLayerProperty();
                    }
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
                case nameof(TextLineColor):
                    TextLineColorBrush = new SolidColorBrush(TextLineColor.ToByteColor());
                    ChangeTextLayerProperty();
                    break;
                case nameof(CurrentTime):
                    UpdateTextPropertyFromLayer();
                    break;
            }
        }

        private void SourceTextPropertyModel_ValueUpdated(object? sender, EventArgs e)
        {
            UpdateTextPropertyFromLayer();
        }
    }

    [UseReactiveProperty]
    abstract partial class FontViewModelBase : BindableBase
    {
        [ReactiveProperty]
        public partial GeometryGroup? Sample { get; private set; }

        protected abstract FontFamily SampleFontFamily { get; }

        public static void CreateSampleGeometry(IEnumerable<FontViewModelBase> viewModels)
        {
            const float FontSize = 20.0F;

            Task.Factory.StartNew(() =>
            {
                foreach (var vm in viewModels)
                {
                    lock (vm)
                    {
                        if (vm.Sample != null)
                        {
                            continue;
                        }

                        var group = new GeometryGroup();
                        var paths = TextBuilder.GenerateGlyphs("参麩靇 さんぷる サンプル Sample", new TextOptions(vm.SampleFontFamily.CreateFont(FontSize)));
                        foreach (var path in paths)
                        {
                            foreach (var simplePath in path.Flatten())
                            {
                                var geometry = new StreamGeometry
                                {
                                    FillRule = FillRule.Nonzero
                                };
                                using (var context = geometry.Open())
                                {
                                    var span = simplePath.Points.Span;
                                    context.BeginFigure(new System.Windows.Point(span[0].X, span[0].Y), true, simplePath.IsClosed);
                                    for (var i = 1; i < span.Length; i++)
                                    {
                                        context.LineTo(new System.Windows.Point(span[i].X, span[i].Y), false, false);
                                    }
                                }
                                group.Children.Add(geometry);
                            }
                        }

                        group.Freeze();


                        // NOTE: 型はnon nullだが、すぐに落とした時等にnullになることがある
                        //       また、nullになるタイミングは不定のため、ループの頭ではなくここでチェックする
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            vm.Sample = group;
                        }), DispatcherPriority.Background);
                        if (Application.Current == null)
                        {
                            return;
                        }
                    }
                }
            });
        }
    }

    class FontGroupViewModel : FontViewModelBase
    {
        protected override FontFamily SampleFontFamily => FontFamily.FontInfos[0].FontFamily;

        public FontGroup FontFamily { get; }

        public FontSubFamilyViewModel[] SubFamiles { get; }

        public string FontName => FontFamily.FontName;

        public FontGroupViewModel(FontGroup fontFamily)
        {
            FontFamily = fontFamily;
            SubFamiles = fontFamily.SubFamiles.Select(kv => new FontSubFamilyViewModel(kv.Value, kv.Key)).ToArray();
        }
    }

    class FontSubFamilyViewModel : FontViewModelBase
    {
        protected override FontFamily SampleFontFamily => FontInfo.FontFamily;

        public FontInfo FontInfo { get; }

        public bool IsSupportBold { get; }

        public bool IsSupportItalic { get; }

        public string SubFamilyName { get; }

        public FontSubFamilyViewModel(FontInfo fontInfo, string subFamilyName)
        {
            FontInfo = fontInfo;
            IsSupportBold = fontInfo.FontFamily.GetAvailableStyles().Contains(FontStyle.Bold);
            IsSupportItalic = fontInfo.FontFamily.GetAvailableStyles().Contains(FontStyle.Italic);
            SubFamilyName = subFamilyName;
        }
    }
}
