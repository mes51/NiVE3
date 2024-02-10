using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;
using ImTools;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Property;
using NiVE3.Property.Types;
using NiVE3.Shape;
using NiVE3.Shared.Extension;
using NiVE3.Text;
using NiVE3.View.Resource;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using Polygon = NiVE3.Shape.Polygon;

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

            var glyphPaths = TextBuilder.GenerateGlyphs(sourceText.Text, textOption);
            var glyphPolygons = new List<(Polygon[] fillPolygins, Polygon[] outlinePolygons, ExtendedTextRun textRun, Vector128<int> rect)>();
            foreach (var (path, i) in glyphPaths.ZipWithIndex())
            {
                if (path.Bounds.Width <= 0.0F || path.Bounds.Height <= 0.0F)
                {
                    continue;
                }

                var styleRun = filledStyles.Find(s => s.Start >= i && s.End < i) ?? new TextStyleRun(0, int.MaxValue, sourceText.DefaultStyle);
                var fillPolygons = path.Flatten().Where(p => p.Points.Length > 1).Select(p => new Polygon(p.Points.Span)).ToArray();
                var outlinePolygons = Array.Empty<Polygon>();
                if (styleRun.Style.TextLineDrawOrder != TextLineDrawOrder.None && styleRun.Style.TextLineWidth > 0.0F)
                {
                    outlinePolygons = path.GenerateOutline(styleRun.Style.TextLineWidth).Flatten().Where(p => p.Points.Length > 1).Select(p => new Polygon(p.Points.Span)).ToArray();
                }

                glyphPolygons.Add((fillPolygons, outlinePolygons, styleRun.ToTextRun(), GetPolygonRect(fillPolygons.Concat(outlinePolygons))));
            }

            var min = Vector128.Create(int.MaxValue);
            var max = Vector128.Create(int.MinValue);
            foreach (var (_, _, _, r) in glyphPolygons)
            {
                min = Sse41.Min(min, r);
                max = Sse41.Max(max, r);
            }

            var image = new NManagedImage(max.GetElement(2) - min.GetElement(0), max.GetElement(3) - min.GetElement(1));
            image.Origin = new Vector2d(min.GetElement(0), min.GetElement(1));
            foreach (var (fillPolygins, outlinePolygons, textRun, rect) in glyphPolygons)
            {
                var intLeft = rect.GetElement(0);
                var intTop = rect.GetElement(1);
                using var glyphImage = new NManagedImage(rect.GetElement(2) - intLeft, rect.GetElement(3) - intTop);
                if (outlinePolygons.Length > 0)
                {
                    switch (textRun.TextLineDrawOrder)
                    {
                        case TextLineDrawOrder.AfterFill:
                            ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, textRun.FillColor, intLeft, intTop);
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textRun.OutlineColor, intLeft, intTop);
                            break;
                        case TextLineDrawOrder.BeforeFill:
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textRun.OutlineColor, intLeft, intTop);
                            ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, textRun.FillColor, intLeft, intTop);
                            break;
                        default:
                            ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, textRun.FillColor, intLeft, intTop);
                            break;
                    }
                }
                else
                {
                    ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, textRun.FillColor, intLeft, intTop);
                }

                DrawImage(image, glyphImage, intLeft - min.GetElement(0), intTop - min.GetElement(1));
            }

            return image;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector128<int> GetPolygonRect(IEnumerable<Polygon> polygons)
        {
            var min = Vector128.Create(int.MaxValue);
            var max = Vector128.Create(int.MinValue);
            foreach (var r in polygons.Select(g => Vector128.Create((int)MathF.Floor(g.MinX), (int)MathF.Floor(g.MinY), (int)MathF.Ceiling(g.MaxX), (int)MathF.Ceiling(g.MaxY))))
            {
                min = Sse41.Min(min, r);
                max = Sse41.Max(max, r);
            }

            return Avx2.Blend(min, max, 0b1100);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DrawImage(NManagedImage back, NManagedImage front, int offsetX, int offsetY)
        {
            Parallel.For(0, front.Height, y =>
            {
                var backSpan = MemoryMarshal.Cast<float, Vector4>(back.GetDataSpan()).Slice((offsetY + y) * back.Width + offsetX);
                var frontSpan = MemoryMarshal.Cast<float, Vector4>(front.GetDataSpan()).Slice(y * front.Width);

                for (var x = 0; x < front.Width; x++)
                {
                    backSpan[x] = Blend.Process(BlendMode.Normal, backSpan[x], frontSpan[x]);
                }
            });
        }
    }
}
