using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Property;
using NiVE3.Property.Types;
using NiVE3.Shape;
using NiVE3.Text;
using NiVE3.View.Resource;
using SixLabors.Fonts;

namespace NiVE3.Input.Special
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(TextInput), nameof(TextInput), "", "mes51", ID, "", false)]
    [InternalInput]
    [SpecialInput]
    class TextInput : IInput
    {
        const string ID = "0B859790-FA6E-4685-AAE2-D80F854B2B9B";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public static TextInput Instance { get; } = new TextInput();

        public string FilePath => "テキスト";

        private TextInput() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup(new IFootageSource[] { TextFootageSource.Instance });
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }

    class TextFootageSource : ICustomizableFootageSource
    {
        public const string SourceTextId = nameof(SourceTextId);

        const string TextMoreOptionsGroupId = nameof(TextMoreOptionsGroupId);

        const string TextBoxSizeId = nameof(TextBoxSizeId);

        const string TextMoreOptionInterCharacterBlendModeId = nameof(TextMoreOptionInterCharacterBlendModeId);

        public static readonly TextFootageSource Instance = new TextFootageSource();

        private TextFootageSource() { }

        public string SourceId => "text";

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.Image;

        public PropertyBase[] GetOptionProperties()
        {
            return new PropertyBase[]
            {
                new SourceTextProperty(SourceTextId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_SourceText), StyledText.Empty),
                new PropertyGroup(TextMoreOptionsGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextMoreOptions), new PropertyBase[]
                {
                    new Vector3dProperty(TextBoxSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextMoreOptions_TextBoxSize), new Vector3d(), digit: 2),
                    new EnumProperty(TextMoreOptionInterCharacterBlendModeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextMoreOptions_InterCharacterBlendMode), typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal)
                })
            };
        }

        public NImage Read(double time, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }

        public NImage Read(double time, int compositionWidth, int compositionHeight, PropertyValueGroup properties, bool toGpu)
        {
            var sourceText = properties[SourceTextId] as StyledText ?? StyledText.Empty;
            if (string.IsNullOrEmpty(sourceText.Text))
            {
                return new NManagedImage(1, 1);
            }

            var textCount = new StringInfo(sourceText.Text).LengthInTextElements;
            var filledStyles = new List<TextStyleRun>();
            foreach (var s in sourceText.Styles)
            {
                var prevGapStart = filledStyles.LastOrDefault()?.End ?? 0;
                var prevGatEnd = s.Start;
                if (prevGapStart < prevGatEnd)
                {
                    filledStyles.Add(new TextStyleRun(prevGapStart, prevGatEnd, sourceText.DefaultStyle));
                }
                filledStyles.Add(s);
            }
            var lastStyleRunEnd = filledStyles.LastOrDefault()?.End ?? 0;
            if (lastStyleRunEnd < textCount)
            {
                filledStyles.Add(new TextStyleRun(lastStyleRunEnd, textCount, sourceText.DefaultStyle));
            }

            var fontInfo = FontInfo.FindByUniqueId(sourceText.DefaultStyle.FontUniqueId) ?? FontInfo.FallbackFont;
            var font = fontInfo.FontFamily.CreateFont((float)sourceText.DefaultStyle.FontSize);
            var textOption = new TextOptions(font);
            textOption.TextRuns = filledStyles.Select(s => s.ToTextRun()).ToArray();

            var glyphPaths = SixLabors.ImageSharp.Drawing.TextBuilder.GenerateGlyphs(sourceText.Text, textOption);
            var glyphPolygons = glyphPaths.Select(g => g.Flatten().Select(p => new Polygon(p.Points.Span)).ToArray()).ToArray();

            var image = new NManagedImage(compositionWidth, compositionHeight);
            foreach (var p in glyphPolygons)
            {
                ShapeRender.FillPolygonNonzero(p, image, Vector4.One);
            }

            return image;
        }
    }
}
