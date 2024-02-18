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

        const string TextAnimatorsId = nameof(TextAnimatorsId);

        const string TextAnimatorAnimatorId = nameof(TextAnimatorAnimatorId);

        const string TextAnimatorSelectorId = nameof(TextAnimatorSelectorId);

        const string TextAnimatorSelectorSelectorId = nameof(TextAnimatorSelectorSelectorId);

        const string TextAnimatorSelectorBeginId = nameof(TextAnimatorSelectorBeginId);

        const string TextAnimatorSelectorEndId = nameof(TextAnimatorSelectorEndId);

        const string TextAnimatorSelectorOffsetId = nameof(TextAnimatorSelectorOffsetId);

        const string TextAnimatorSelectorMoreOptionId = nameof(TextAnimatorSelectorMoreOptionId);

        const string TextAnimatorSelectorCriteriaId = nameof(TextAnimatorSelectorCriteriaId);

        const string TextAnimatorSelectorBlendModeId = nameof(TextAnimatorSelectorBlendModeId);

        const string TextAnimatorSelectorAmountId = nameof(TextAnimatorSelectorAmountId);

        const string TextAnimatorSelectorShapeId = nameof(TextAnimatorSelectorShapeId);

        const string TextAnimatorSelectorEnableRandomId = nameof(TextAnimatorSelectorEnableRandomId);

        const string TextAnimatorSelectorRandomSeedId = nameof(TextAnimatorSelectorRandomSeedId);

        const string TextAnimatorValueId = nameof(TextAnimatorValueId);

        const string TextAnimatorValueAnchorPointId = nameof(TextAnimatorValueAnchorPointId);

        const string TextAnimatorValuePositionId = nameof(TextAnimatorValuePositionId);

        const string TextAnimatorValueScaleId = nameof(TextAnimatorValueScaleId);

        const string TextAnimatorValueRotateId = nameof(TextAnimatorValueRotateId);

        const string TextAnimatorValueSkewId = nameof(TextAnimatorValueSkewId);

        const string TextAnimatorValueSkewAxisId = nameof(TextAnimatorValueSkewAxisId);

        const string TextAnimatorValueOpacityId = nameof(TextAnimatorValueOpacityId);

        const string TextAnimatorValueFontSizeId = nameof(TextAnimatorValueFontSizeId);

        const string TextAnimatorValueFillColorId = nameof(TextAnimatorValueFillColorId);

        const string TextAnimatorValueTextLineColorId = nameof(TextAnimatorValueTextLineColorId);

        const string TextAnimatorValueTextLineWidthId = nameof(TextAnimatorValueTextLineWidthId);

        const string TextAnimatorValueCharacterOffsetId = nameof(TextAnimatorValueCharacterOffsetId);

        const string TextAnimatorValueBlurId = nameof(TextAnimatorValueBlurId);

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
                    new Vector3dProperty(TextBoxSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextMoreOptions_TextBoxSize), new Vector3d(), new Vector3d(), digit: 2),
                    new EnumProperty(TextMoreOptionInterCharacterBlendModeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextMoreOptions_InterCharacterBlendMode), typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal)
                }),
                new AppendableProperty(TextAnimatorsId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator), new AppendablePropertyItem[]
                {
                    new AppendablePropertyItem(TextAnimatorAnimatorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator), () =>
                        new PropertyGroup(TextAnimatorAnimatorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator), new PropertyBase[]
                        {
                            new AppendableProperty(TextAnimatorSelectorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector), new AppendablePropertyItem[]
                            {
                                new AppendablePropertyItem(TextAnimatorSelectorSelectorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Selector), () =>
                                    new PropertyGroup(TextAnimatorSelectorSelectorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Selector), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorSelectorBeginId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Begin), 0.0, -100.0, 100.0, digit: 2),
                                        new DoubleProperty(TextAnimatorSelectorEndId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_End), 100.0, -100.0, 100.0, digit: 2),
                                        new DoubleProperty(TextAnimatorSelectorOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Offset), 0.0, -100.0, 100.0, digit: 2),
                                        new PropertyGroup(TextAnimatorSelectorMoreOptionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_MoreOption), new PropertyBase[]
                                        {
                                            new EnumProperty(TextAnimatorSelectorCriteriaId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Criteria), typeof(SelectorCriteria), typeof(LanguageResourceDictionary), SelectorCriteria.Charactor, selectBoxWidth: 125.0),
                                            new EnumProperty(TextAnimatorSelectorBlendModeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_BlendMode), typeof(SelectorBlendMode), typeof(LanguageResourceDictionary), SelectorBlendMode.Add, selectBoxWidth: 125.0),
                                            new DoubleProperty(TextAnimatorSelectorAmountId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Amount), 100.0, 0.0, 100.0, digit: 2),
                                            new EnumProperty(TextAnimatorSelectorShapeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Shape), typeof(SelectorShape), typeof(LanguageResourceDictionary), SelectorShape.Rectangle, selectBoxWidth: 125.0),
                                            new CheckBoxProperty(TextAnimatorSelectorEnableRandomId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_EnableRandom), false),
                                            new DoubleProperty(TextAnimatorSelectorRandomSeedId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_RandomSeed), 0, double.MinValue, double.MaxValue, digit: 0)
                                        })
                                    }))
                            }, 0),
                            new AppendableProperty(TextAnimatorValueId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value), new AppendablePropertyItem[]
                            {
                                new AppendablePropertyItem(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_AnchorPoint), () =>
                                    new PropertyGroup(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_AnchorPoint), new PropertyBase[]
                                    {
                                        new Vector3dProperty(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_AnchorPoint), new Vector3d(), digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValuePositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Position), () =>
                                    new PropertyGroup(TextAnimatorValuePositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Position), new PropertyBase[]
                                    {
                                        new Vector3dProperty(TextAnimatorValuePositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Position), new Vector3d(), digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueScaleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Scale), () =>
                                    new PropertyGroup(TextAnimatorValueScaleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Scale), new PropertyBase[]
                                    {
                                        new Scale3dProperty(TextAnimatorValueScaleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Scale), new Vector3d(100.0), digit: 2, is3D: false)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueRotateId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Rotate), () =>
                                    new PropertyGroup(TextAnimatorValueRotateId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Rotate), new PropertyBase[]
                                    {
                                        new AngleProperty(TextAnimatorValueRotateId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Rotate), 0.0, digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueSkewId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Skew), () =>
                                    new PropertyGroup(TextAnimatorValueSkewId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Skew), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorValueSkewId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Skew), 0.0, -100.0, 100.0, digit: 2),
                                        new AngleProperty(TextAnimatorValueSkewAxisId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_SkewAxis), 0.0, digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Opacity), () =>
                                    new PropertyGroup(TextAnimatorValueOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Opacity), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorValueOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Opacity), 0.0, 0.0, 100.0, digit: 2)
                                    })),
                                AppendablePropertyItemSeparator.Instance,
                                new AppendablePropertyItem(TextAnimatorValueFontSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize), () =>
                                    new PropertyGroup(TextAnimatorValueFontSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorValueFontSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize), 20.0, 0.0, double.MaxValue, digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueFillColorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FillColor), () =>
                                    new PropertyGroup(TextAnimatorValueFillColorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FillColor), new PropertyBase[]
                                    {
                                        new ColorProperty(
                                            TextAnimatorValueFillColorId,
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FillColor),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FillColor),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_OK),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_Cancel),
                                            Vector4.One
                                        )
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueTextLineColorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineColor), () =>
                                    new PropertyGroup(TextAnimatorValueTextLineColorId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineColor), new PropertyBase[]
                                    {
                                        new ColorProperty(
                                            TextAnimatorValueTextLineColorId,
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineColor),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineColor),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_OK),
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_Cancel),
                                            new Vector4(0.0F, 0.0F, 1.0F, 1.0F)
                                        )
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth), () =>
                                    new PropertyGroup(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth), 3.0, 0.0, double.MaxValue, digit: 2)
                                    })),
                                AppendablePropertyItemSeparator.Instance,
                                new AppendablePropertyItem(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), () =>
                                    new PropertyGroup(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), new PropertyBase[]
                                    {
                                        new DoubleProperty(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), 0.0, 0.0, ushort.MaxValue, digit: 0)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), () =>
                                    new PropertyGroup(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), new PropertyBase[]
                                    {
                                        new Vector3dProperty(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), new Vector3d(), new Vector3d(), digit: 2, is3D: false)
                                    })),
                            })
                        }))
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
            var wrappingSize = (Vector3d)((properties[TextMoreOptionsGroupId] as PropertyValueGroup)?[TextBoxSizeId] ?? new Vector3d());
            textOption.TextRuns = filledStyles.Select(s => s.ToTextRun()).ToArray();
            textOption.WrappingLength = wrappingSize.X > 0.0 ? (float)wrappingSize.X : -1.0F;
            textOption.WordBreaking = wrappingSize.X > 0.0 ? WordBreaking.BreakAll : WordBreaking.Standard;

            var glyphBuilder = new StyledGlyphBuilder((float)wrappingSize.X, (float)wrappingSize.Y);
            TextRenderer.RenderTextTo(glyphBuilder, sourceText.Text, textOption);
            var glyphPolygons = new List<(Polygon[] fillPolygins, Polygon[] outlinePolygons, ExtendedTextRun textRun, Vector128<int> rect, Vector2 origin)>();
            foreach (var glyph in glyphBuilder.GetRenderableGlyhps())
            {
                var fillPolygons = glyph.FlattenedPath.Select(p => new Polygon(p.Points.Span)).ToArray();
                var outlinePolygons = glyph.FlattenedOutlinePath.Select(p => new Polygon(p.Points.Span)).ToArray();

                var fillRect = GetPolygonRect(fillPolygons);
                var outlineRect = GetPolygonRect(outlinePolygons);
                var rect = Vector128.Create(
                    Math.Min(fillRect.GetElement(0), outlineRect.GetElement(0)),
                    Math.Min(fillRect.GetElement(1), outlineRect.GetElement(1)),
                    Math.Max(fillRect.GetElement(2), outlineRect.GetElement(2)),
                    Math.Max(fillRect.GetElement(3), outlineRect.GetElement(3))
                );
                var fillOrigin = new Vector2(fillRect.GetElement(0), fillRect.GetElement(1)); //fillPolygons.Select(p => new Vector2(p.MinX, p.MinY)).Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                var origin = -fillOrigin + (outlinePolygons.Length > 0 ? fillOrigin - new Vector2(outlineRect.GetElement(0), outlineRect.GetElement(1)) : Vector2.Zero);
                glyphPolygons.Add((fillPolygons, outlinePolygons, glyph.TextRun, rect, origin));
            }

            if (glyphPolygons.Count < 1)
            {
                return new NManagedImage(1, 1);
            }

            var min = Vector128.Create(int.MaxValue);
            var max = Vector128.Create(int.MinValue);
            foreach (var (_, _, _, r, _) in glyphPolygons)
            {
                min = Sse41.Min(min, r);
                max = Sse41.Max(max, r);
            }

            var interCharBlendMode = (BlendMode)((properties[TextMoreOptionsGroupId] as PropertyValueGroup)?[TextMoreOptionInterCharacterBlendModeId] ?? BlendMode.Normal);
            var image = new NManagedImage(max.GetElement(2) - min.GetElement(0), max.GetElement(3) - min.GetElement(1));
            image.Origin = (Vector2d)glyphPolygons[0].origin + new Vector2d(glyphPolygons[0].rect.GetElement(0) - min.GetElement(0), glyphPolygons[0].rect.GetElement(1) - min.GetElement(1));
            foreach (var (fillPolygins, outlinePolygons, textRun, rect, _) in glyphPolygons)
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
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textRun.TextLineColor, intLeft, intTop);
                            break;
                        case TextLineDrawOrder.BeforeFill:
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textRun.TextLineColor, intLeft, intTop);
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

                DrawImage(interCharBlendMode, image, glyphImage, intLeft - min.GetElement(0), intTop - min.GetElement(1));
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
        static void DrawImage(BlendMode blendMode, NManagedImage back, NManagedImage front, int offsetX, int offsetY)
        {
            Parallel.For(0, front.Height, y =>
            {
                var backSpan = MemoryMarshal.Cast<float, Vector4>(back.GetDataSpan()).Slice((offsetY + y) * back.Width + offsetX);
                var frontSpan = MemoryMarshal.Cast<float, Vector4>(front.GetDataSpan()).Slice(y * front.Width);

                for (var x = 0; x < front.Width; x++)
                {
                    backSpan[x] = Blend.Process(blendMode, backSpan[x], frontSpan[x]);
                }
            });
        }
    }

    enum SelectorCriteria
    {
        Charactor,
        CharactorWithoutSpace,
        Word,
        Line
    }

    enum SelectorBlendMode
    {
        Add,
        Subtract,
        Multiply,
        Min,
        Max,
        Difference
    }

    enum SelectorShape
    {
        Rectangle,
        RampUp,
        RampDown,
        Triangle,
        Circle
    }
}
