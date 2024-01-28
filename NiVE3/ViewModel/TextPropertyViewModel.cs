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
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Mvvm;
using SixLabors.ImageSharp.Drawing;
using FontFamily = SixLabors.Fonts.FontFamily;
using TextOptions = SixLabors.Fonts.TextOptions;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right1Center)]
    class TextPropertyViewModel : SingletonePaneViewModelBase
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

        public FontGroupViewModel SelectedFontGroup => Fonts[SelectedFontGroupIndex];

        public List<FontGroupViewModel> Fonts { get; set; } = new List<FontGroupViewModel>();

        TextPropertyModel TextPropertyModel { get; }

        public TextPropertyViewModel(TextPropertyModel textPropertyModel)
        {
            TextPropertyModel = textPropertyModel;
            Fonts.AddRange(textPropertyModel.FontGroups.Select(f => new FontGroupViewModel(f)));

            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.TextPropertyView_Title);

            PropertyChanged += TextPropertyViewModel_PropertyChanged;
        }

        private void TextPropertyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedFontGroupIndex):
                    RaisePropertyChanged(nameof(SelectedFontGroup));
                    SelectedFontSubFamilyIndex = 0;
                    break;
            }
        }
    }

    class FontGroupViewModel : BindableBase
    {
        static readonly SemaphoreSlim SampleCreationSemaphoe = new SemaphoreSlim(1);

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
                SampleCreationSemaphoe.Wait();
                try
                {
                    if (sample != null)
                    {
                        return;
                    }

                    var font = FontFamily.FontInfos[0].FontFamily;
                    var paths = TextBuilder.GenerateGlyphs("参麩靇 サンプル Sample", new TextOptions(font.CreateFont(FontSize)));
                    var collection = new GeometryCollection();
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

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Sample = collection;
                    }));
                }
                finally
                {
                    SampleCreationSemaphoe.Release();
                }
            });
        }
    }

    class FontSubFamilyViewModel : BindableBase
    {
        static readonly SemaphoreSlim SampleCreationSemaphoe = new SemaphoreSlim(1);

        public FontInfo FontInfo { get; }

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
            SubFamilyName = subFamilyName;
        }

        void CreateSampleGeometry()
        {
            const float FontSize = 20.0F;

            Task.Factory.StartNew(() =>
            {
                SampleCreationSemaphoe.Wait();
                try
                {
                    if (sample != null)
                    {
                        return;
                    }

                    var font = FontInfo.FontFamily;
                    var paths = TextBuilder.GenerateGlyphs("参麩靇 サンプル Sample", new TextOptions(font.CreateFont(FontSize)));
                    var collection = new GeometryCollection();
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

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Sample = collection;
                    }));
                }
                finally
                {
                    SampleCreationSemaphoe.Release();
                }
            });
        }
    }
}
