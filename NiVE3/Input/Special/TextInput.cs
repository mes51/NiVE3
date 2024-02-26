using System;
using System.Buffers;
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
using NiVE3.Extension;
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
using NiVE3.Shared.Util;
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

        const string TextAnimatorValueAngleId = nameof(TextAnimatorValueAngleId);

        const string TextAnimatorValueSkewId = nameof(TextAnimatorValueSkewId);

        const string TextAnimatorValueSkewAxisId = nameof(TextAnimatorValueSkewAxisId);

        const string TextAnimatorValueOpacityId = nameof(TextAnimatorValueOpacityId);

        const string TextAnimatorValueFontSizeId = nameof(TextAnimatorValueFontSizeId);

        const string TextAnimatorValueFillColorId = nameof(TextAnimatorValueFillColorId);

        const string TextAnimatorValueFillColorOpacityId = nameof(TextAnimatorValueFillColorOpacityId);

        const string TextAnimatorValueTextLineColorId = nameof(TextAnimatorValueTextLineColorId);

        const string TextAnimatorValueTextLineColorOpacityId = nameof(TextAnimatorValueTextLineColorOpacityId );

        const string TextAnimatorValueTextLineWidthId = nameof(TextAnimatorValueTextLineWidthId);

        // NOTE: 混ざるとレイアウトシステムが例外を吐くものがあるが、どれが混ざるとNGなのか判定する術が無いため一旦omit
        //const string TextAnimatorValueCharacterOffsetId = nameof(TextAnimatorValueCharacterOffsetId);

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
                                        new DoubleProperty(TextAnimatorSelectorBeginId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Begin), 0.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                                        new DoubleProperty(TextAnimatorSelectorEndId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_End), 100.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                                        new DoubleProperty(TextAnimatorSelectorOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Selector_Offset), 0.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
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
                                new AppendablePropertyItem(TextAnimatorValueAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Angle), () =>
                                    new PropertyGroup(TextAnimatorValueAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Angle), new PropertyBase[]
                                    {
                                        new AngleProperty(TextAnimatorValueAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Angle), 0.0, digit: 2)
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
                                        new DoubleProperty(TextAnimatorValueOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Opacity), 100.0, 0.0, 100.0, digit: 2)
                                    })),
                                AppendablePropertyItemSeparator.Instance,
                                new AppendablePropertyItem(TextAnimatorValueFontSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize), () =>
                                    new PropertyGroup(TextAnimatorValueFontSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize), new PropertyBase[]
                                    {
                                        new DoubleProperty(
                                            TextAnimatorValueFontSizeId,
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FontSize),
                                            0.0,
                                            double.MinValue,
                                            double.MaxValue,
                                            digit: 2,
                                            unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Pixel)
                                        )
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
                                        ),
                                        new DoubleProperty(TextAnimatorValueFillColorOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_FillColorOpacity), 100.0, 0.0, 100.0, digit: 2)
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
                                        ),
                                        new DoubleProperty(TextAnimatorValueTextLineColorOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity), 100.0, 0.0, 100.0, digit: 2)
                                    })),
                                new AppendablePropertyItem(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth), () =>
                                    new PropertyGroup(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth), new PropertyBase[]
                                    {
                                        new DoubleProperty(
                                            TextAnimatorValueTextLineWidthId,
                                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_TextLineWidth),
                                            3.0,
                                            double.MinValue,
                                            double.MaxValue,
                                            digit: 2,
                                            unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Pixel)
                                        )
                                    })),
                                AppendablePropertyItemSeparator.Instance,
                                //new AppendablePropertyItem(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), () =>
                                //    new PropertyGroup(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), new PropertyBase[]
                                //    {
                                //        new DoubleProperty(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_CharacterOffset), 0.0, 0.0, ushort.MaxValue, digit: 0)
                                //    })),
                                new AppendablePropertyItem(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), () =>
                                    new PropertyGroup(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), new PropertyBase[]
                                    {
                                        new Vector3dProperty(TextAnimatorValueBlurId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.TextProperty_TextAnimator_Animator_Value_Blur), new Vector3d(), digit: 2, is3D: false)
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
            var structuredExtendedTextRun = new StructuredExtendedTextRun(sourceText.Text, sourceText.DefaultStyle, sourceText.Styles);
            foreach (var animator in ((PropertyValueGroup[])(properties[TextAnimatorsId] ?? Array.Empty<PropertyValueGroup>())))
            {
                ApplyAnimator(structuredExtendedTextRun, animator);
            }

            var fontInfo = FontInfo.FindByUniqueId(sourceText.DefaultStyle.FontUniqueId) ?? FontInfo.FallbackFont;
            var font = fontInfo.FontFamily.CreateFont((float)sourceText.DefaultStyle.FontSize);
            var textOption = new TextOptions(font);
            var wrappingSize = (Vector3d)((properties[TextMoreOptionsGroupId] as PropertyValueGroup)?[TextBoxSizeId] ?? new Vector3d());
            textOption.WrappingLength = wrappingSize.X > 0.0 ? (float)wrappingSize.X : -1.0F;
            textOption.WordBreaking = wrappingSize.X > 0.0 ? WordBreaking.BreakAll : WordBreaking.Standard;
            textOption.TextRuns = structuredExtendedTextRun.Flatten();

            var glyphBuilder = new StyledGlyphBuilder((float)wrappingSize.X, (float)wrappingSize.Y);
            TextRenderer.RenderTextTo(glyphBuilder, sourceText.Text, textOption);
            var glyphPolygons = new List<(Polygon[] fillPolygins, Polygon[] outlinePolygons, ExtendedTextRun textRun, Vector128<int> rect, Vector128<int> blurMargin, Vector2 origin)>();
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
                var blurMargin = Vector128.Create(
                    (int)Math.Ceiling(glyph.TextRun.Blur.X),
                    (int)Math.Ceiling(glyph.TextRun.Blur.Y),
                    (int)Math.Ceiling(glyph.TextRun.Blur.X),
                    (int)Math.Ceiling(glyph.TextRun.Blur.Y)
                );
                var fillOrigin = new Vector2(fillRect.GetElement(0), fillRect.GetElement(1)); //fillPolygons.Select(p => new Vector2(p.MinX, p.MinY)).Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                var origin = -fillOrigin + (outlinePolygons.Length > 0 ? fillOrigin - new Vector2(outlineRect.GetElement(0), outlineRect.GetElement(1)) : Vector2.Zero);
                glyphPolygons.Add((fillPolygons, outlinePolygons, glyph.TextRun, rect, blurMargin, origin));
            }

            if (glyphPolygons.Count < 1)
            {
                return new NManagedImage(1, 1);
            }

            var min = Vector128.Create(int.MaxValue);
            var max = Vector128.Create(int.MinValue);
            foreach (var (_, _, _, r, blurMargin, _) in glyphPolygons)
            {
                min = Sse41.Min(min, Sse2.Subtract(r, blurMargin));
                max = Sse41.Max(max, Sse2.Add(r, blurMargin));
            }

            var interCharBlendMode = (BlendMode)((properties[TextMoreOptionsGroupId] as PropertyValueGroup)?[TextMoreOptionInterCharacterBlendModeId] ?? BlendMode.Normal);
            var image = new NManagedImage(max.GetElement(2) - min.GetElement(0), max.GetElement(3) - min.GetElement(1));
            image.Origin = (Vector2d)glyphPolygons[0].origin + new Vector2d(glyphPolygons[0].rect.GetElement(0) - min.GetElement(0), glyphPolygons[0].rect.GetElement(1) - min.GetElement(1));
            foreach (var (fillPolygins, outlinePolygons, textRun, rect, blurMargin, _) in glyphPolygons)
            {
                var fillColor = textRun.FillColor;
                var textLineColor = textRun.TextLineColor;
                if (textRun.Opacity <= 0.0F || (fillColor.W <= 0.0F && (outlinePolygons.Length < 1 || textLineColor.W <= 0.0F)))
                {
                    continue;
                }

                var intLeft = rect.GetElement(0);
                var intTop = rect.GetElement(1);
                using var glyphImage = new NManagedImage(rect.GetElement(2) - intLeft + blurMargin.GetElement(0) * 2, rect.GetElement(3) - intTop + blurMargin.GetElement(1) * 2);
                var glyphLeft = intLeft - blurMargin.GetElement(0);
                var glyphTop = intTop - blurMargin.GetElement(1);
                if (outlinePolygons.Length > 0 && textLineColor.W > 0.0F)
                {
                    switch (textRun.TextLineDrawOrder)
                    {
                        case TextLineDrawOrder.AfterFill:
                            if (fillColor.W > 0.0F)
                            {
                                ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, fillColor, glyphLeft, glyphTop);
                            }
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop);
                            break;
                        default:
                            ShapeRender.FillPolygonNonzero(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop);
                            if (fillColor.W > 0.0F)
                            {
                                ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, fillColor, glyphLeft, glyphTop);
                            }
                            break;
                    }
                }
                else
                {
                    ShapeRender.FillPolygonNonzero(fillPolygins, glyphImage, fillColor, glyphLeft, glyphTop);
                }

                if (textRun.Blur != Vector2.Zero)
                {
                    Blur.BoxBlur(glyphImage, textRun.Blur.X, textRun.Blur.Y);
                }
                DrawImage(interCharBlendMode, image, glyphImage, textRun.Opacity, intLeft - min.GetElement(0) - blurMargin.GetElement(0), intTop - min.GetElement(1) - blurMargin.GetElement(1));
            }

            return image;
        }

        static void ApplyAnimator(StructuredExtendedTextRun structuredExtendedTextRun, PropertyValueGroup animatorPropertyValue)
        {
            var selected = ArrayPool<double>.Shared.Rent(structuredExtendedTextRun.TotalElementCountWithoutNewLine);
            var currentSelection = ArrayPool<double>.Shared.Rent(structuredExtendedTextRun.TotalElementCountWithoutNewLine);
            var selectedSpan = selected.AsSpan(0, structuredExtendedTextRun.TotalElementCountWithoutNewLine);
            var currentSelectionSpan = currentSelection.AsSpan(0, structuredExtendedTextRun.TotalElementCountWithoutNewLine);
            selectedSpan.Clear();

            foreach (var selector in (PropertyValueGroup[])(animatorPropertyValue[TextAnimatorSelectorId] ?? Array.Empty<PropertyValueGroup>()))
            {
                var moreOption = (PropertyValueGroup)(selector[TextAnimatorSelectorMoreOptionId] ?? PropertyValueGroup.Empty);
                Func<double, double, double, double, double> shapeGenerator = (SelectorShape)(moreOption[TextAnimatorSelectorShapeId] ?? SelectorShape.Rectangle) switch
                {
                    SelectorShape.RampUp => GetSelectShapeRampUp,
                    SelectorShape.RampDown => GetSelectShapeRampDown,
                    SelectorShape.Triangle => GetSelectShapeTriangle,
                    SelectorShape.Circle => GetSelectShapeCircle,
                    _ => GetSelectShapeRectangle,
                };

                var begin = (double)(selector[TextAnimatorSelectorBeginId] ?? 0.0);
                var end = (double)(selector[TextAnimatorSelectorEndId] ?? 0.0);
                var offset = (double)(selector[TextAnimatorSelectorOffsetId] ?? 0.0);
                if (begin > end)
                {
                    var t = begin;
                    begin = end;
                    end = begin;
                }
                begin = (begin + offset) * 0.01;
                end = (end + offset) * 0.01;

                var useRandom = (bool)(moreOption[TextAnimatorSelectorEnableRandomId] ?? false);
                var randomSeed = (int)(double)(moreOption[TextAnimatorSelectorRandomSeedId] ?? 0.0);
                var amount = (double)(moreOption[TextAnimatorSelectorAmountId] ?? 100.0) * 0.01;
                currentSelectionSpan.Clear();

                switch ((SelectorCriteria)(moreOption[TextAnimatorSelectorCriteriaId] ?? SelectorCriteria.Charactor))
                {
                    case SelectorCriteria.CharactorWithoutSpace:
                        SelectCharacterWithoutSpace(structuredExtendedTextRun.TextElements, currentSelectionSpan, begin, end, useRandom, randomSeed, amount, shapeGenerator);
                        break;
                    case SelectorCriteria.Word:
                        SelectWord(structuredExtendedTextRun.TextElements, currentSelectionSpan, begin, end, useRandom, randomSeed, amount, shapeGenerator);
                        break;
                    case SelectorCriteria.Line:
                        SelectLine(structuredExtendedTextRun.TextElements, currentSelectionSpan, begin, end, useRandom, randomSeed, amount, shapeGenerator);
                        break;
                    default:
                        SelectCharacter(structuredExtendedTextRun.TextElements, currentSelectionSpan, begin, end, useRandom, randomSeed, amount, shapeGenerator);
                        break;
                }

                switch ((SelectorBlendMode)(moreOption[TextAnimatorSelectorBlendModeId] ?? SelectorBlendMode.Add))
                {
                    case SelectorBlendMode.Subtract:
                        SelectionBlendSubtract(selectedSpan, currentSelectionSpan);
                        break;
                    case SelectorBlendMode.Multiply:
                        SelectionBlendMultiply(selectedSpan, currentSelectionSpan);
                        break;
                    case SelectorBlendMode.Min:
                        SelectionBlendMin(selectedSpan, currentSelectionSpan);
                        break;
                    case SelectorBlendMode.Max:
                        SelectionBlendMax(selectedSpan, currentSelectionSpan);
                        break;
                    case SelectorBlendMode.Difference:
                        SelectionBlendDifference(selectedSpan, currentSelectionSpan);
                        break;
                    default:
                        SelectionBlendAdd(selectedSpan, currentSelectionSpan);
                        break;
                }
            }

            var textRuns = structuredExtendedTextRun.GetAllRuns();
            foreach (var animator in (PropertyValueGroup[])(animatorPropertyValue[TextAnimatorValueId] ?? Array.Empty<PropertyValueGroup>()))
            {
                if (animator.TryGetValue<Vector3d>(TextAnimatorValueAnchorPointId, out var anchorPoint))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        textRuns[i].AnchorPoint += (Vector2)(anchorPoint * selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<Vector3d>(TextAnimatorValuePositionId, out var position))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        textRuns[i].Position += (Vector2)(position * selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<Vector3d>(TextAnimatorValueScaleId, out var scale))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.Scale = Vector2.Lerp(run.Scale, (Vector2)scale * 0.01F * run.Scale, (float)selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<double>(TextAnimatorValueAngleId, out var angle))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        textRuns[i].Angle += (float)(angle * selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<double>(TextAnimatorValueSkewId, out var skew))
                {
                    var skewAxis = (float)(double)(animator[TextAnimatorValueSkewAxisId] ?? 0.0);
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.Skew += (float)(skew * selectedSpan[i] * 0.01);
                        run.SkewAxis += (float)(skewAxis * selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<double>(TextAnimatorValueOpacityId, out var opacity))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.Opacity = run.Opacity.Lerp((float)opacity * 0.01F, (float)selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<double>(TextAnimatorValueFontSizeId, out var fontSize))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        if (run.Font != null)
                        {
                            var baseFont = run.Font;
                            var style = (baseFont.IsBold, baseFont.IsItalic) switch
                            {
                                (true, true) => FontStyle.BoldItalic,
                                (true, false) => FontStyle.Bold,
                                (false, true) => FontStyle.Italic,
                                _ => FontStyle.Regular
                            };
                            run.Font = baseFont.Family.CreateFont(Math.Max(baseFont.Size + (float)(fontSize * selectedSpan[i]), 0.0F), style);
                        }
                    }
                }
                else if (animator.TryGetValue<Vector4>(TextAnimatorValueFillColorId, out var fillColor))
                {
                    fillColor.W = (float)(double)(animator[TextAnimatorValueFillColorOpacityId] ?? 0.0) * 0.01F;
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.FillColor = Vector4.Lerp(run.FillColor, fillColor, (float)selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<Vector4>(TextAnimatorValueTextLineColorId, out var lineColor))
                {
                    lineColor.W = (float)(double)(animator[TextAnimatorValueTextLineColorOpacityId] ?? 0.0) * 0.01F;
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.TextLineColor = Vector4.Lerp(run.TextLineColor, lineColor, (float)selectedSpan[i]);
                    }
                }
                else if (animator.TryGetValue<double>(TextAnimatorValueTextLineWidthId, out var textLineWidth))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.TextLineWidth = Math.Max(run.TextLineWidth + (float)(textLineWidth * selectedSpan[i]), 0.0F);
                    }
                }
                //else if (animator.TryGetValue<double>(TextAnimatorValueCharacterOffsetId, out var characterOffset))
                //{
                //    for (var i = 0; i < textRuns.Length; i++)
                //    {
                //        textRuns[i].CharacterOffset += (int)(characterOffset * selectedSpan[i]);
                //    }
                //}
                else if (animator.TryGetValue<Vector3d>(TextAnimatorValueBlurId, out var blur))
                {
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        var run = textRuns[i];
                        run.Blur = Vector2.Max(run.Blur + (Vector2)(blur * selectedSpan[i]), Vector2.Zero);
                    }
                }
            }

            ArrayPool<double>.Shared.Return(currentSelection);
            ArrayPool<double>.Shared.Return(selected);
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
        static void DrawImage(BlendMode blendMode, NManagedImage back, NManagedImage front, float opacity, int offsetX, int offsetY)
        {
            Parallel.For(0, front.Height, y =>
            {
                var backSpan = MemoryMarshal.Cast<float, Vector4>(back.GetDataSpan()).Slice((offsetY + y) * back.Width + offsetX);
                var frontSpan = MemoryMarshal.Cast<float, Vector4>(front.GetDataSpan()).Slice(y * front.Width);

                for (var x = 0; x < front.Width; x++)
                {
                    backSpan[x] = Blend.Process(blendMode, backSpan[x], frontSpan[x] * new Vector4(1.0F, 1.0F, 1.0F, opacity));
                }
            });
        }

        static double GetSelectShapeRampUp(double value, double begin, double end, double align)
        {
            return Math.Clamp((value + align * 0.5 - begin) / (end - begin), 0.0, 1.0);
        }

        static double GetSelectShapeRampDown(double value, double begin, double end, double align)
        {
            return 1.0 - GetSelectShapeRampUp(value, begin, end, align);
        }

        static double GetSelectShapeTriangle(double value, double begin, double end, double align)
        {
            return Math.Clamp(1.0 - Math.Abs((value + align * 0.5 - begin) * 2.0 / (end - begin) - 1.0), 0.0, 1.0);
        }

        static double GetSelectShapeCircle(double value, double begin, double end, double align)
        {
            var t = Math.Clamp((value + align * 0.5 - begin) / (end - begin) * 2.0 - 1.0, -1.0, 1.0);
            return Math.Clamp(Math.Sqrt(1.0 - t * t), 0.0, 1.0);
        }

        static double GetSelectShapeRectangle(double value, double begin, double end, double align)
        {
            return (Math.Clamp(value - begin, -align, 0.0) + Math.Clamp(end - value, 0.0, align)) / align;
        }

        static void SelectCharacter(string[] elements, Span<double> selection, double begin, double end, bool useRandom, int randomSeed, double amount, Func<double, double, double, double, double> shapeGenerator)
        {
            elements = elements.Where(c => !c.Contains('\n')).ToArray();
            var charRange = 1.0 / selection.Length;
            if (double.IsNaN(charRange))
            {
                return;
            }

            var indices = Enumerable.Range(0, elements.Length).ToArray();
            if (useRandom)
            {
                new Xoroshiro(randomSeed).Shuffle(indices);
            }
            var value = 0.0;
            for (var i = 0; i < indices.Length; i++, value += charRange)
            {
                selection[indices[i]] = shapeGenerator(value, begin, end, charRange) * amount;
            }
        }

        static void SelectCharacterWithoutSpace(string[] elements, Span<double> selection, double begin, double end, bool useRandom, int randomSeed, double amount, Func<double, double, double, double, double> shapeGenerator)
        {
            elements = elements.Where(c => !c.Contains('\n')).ToArray();
            var charRange = 1.0 / (selection.Length - elements.Count(c => c.Contains(' ') || c.Contains('　')));
            if (double.IsNaN(charRange))
            {
                return;
            }

            var indices = Enumerable.Range(0, elements.Length).ToArray();
            if (useRandom)
            {
                new Xoroshiro(randomSeed).Shuffle(indices);
            }
            var value = 0.0;
            for (var i = 0; i < indices.Length; i++, value += charRange)
            {
                if (elements[i][0] == ' ' || elements[i][0] == '　')
                {
                    if (i > 0)
                    {
                        selection[indices[i]] = selection[indices[i - 1]];
                    }
                    value -= charRange;
                }
                else
                {
                    selection[indices[i]] = shapeGenerator(value, begin, end, charRange) * amount;
                }
            }
        }

        static void SelectWord(string[] elements, Span<double> selection, double begin, double end, bool useRandom, int randomSeed, double amount, Func<double, double, double, double, double> shapeGenerator)
        {
            var words = new List<List<int>>();
            var index = 0;
            var currentWord = new List<int>();
            foreach (var line in elements.GroupWhile(c => !c.Contains('\n')))
            {
                foreach (var c in line)
                {
                    if ((c[0] == ' ' || c[0] == '　'))
                    {
                        if (currentWord.Count > 0)
                        {
                            words.Add(currentWord);
                            currentWord = new List<int>();
                        }
                        else if (words.Count > 0)
                        {
                            words.Last().Add(index);
                        }
                    }
                    else
                    {
                        currentWord.Add(index);
                    }
                    index++;
                }

                words.Add(currentWord);
                currentWord = new List<int>();
            }

            var charRange = 1.0 / words.Count;
            if (double.IsNaN(charRange))
            {
                return;
            }

            var indices = Enumerable.Range(0, words.Count).ToArray();
            if (useRandom)
            {
                new Xoroshiro(randomSeed).Shuffle(indices);
            }
            var value = 0.0;
            for (var i = 0; i < words.Count; i++, value += charRange)
            {
                var selectValue = shapeGenerator(value, begin, end, charRange) * amount;
                foreach (var ci in words[indices[i]])
                {
                    selection[ci] = selectValue;
                }
            }
        }

        static void SelectLine(string[] elements, Span<double> selection, double begin, double end, bool useRandom, int randomSeed, double amount, Func<double, double, double, double, double> shapeGenerator)
        {
            var lines = new List<List<int>>();
            var index = 0;
            var currentLine = new List<int>();
            foreach (var line in elements.GroupWhile(c => !c.Contains('\n')))
            {
                foreach (var c in line)
                {
                    currentLine.Add(index);
                    index++;
                }

                lines.Add(currentLine);
                currentLine = new List<int>();
            }

            var charRange = 1.0 / lines.Count;
            if (double.IsNaN(charRange))
            {
                return;
            }

            var indices = Enumerable.Range(0, lines.Count).ToArray();
            if (useRandom)
            {
                new Xoroshiro(randomSeed).Shuffle(indices);
            }
            var value = 0.0;
            for (var i = 0; i < lines.Count; i++, value += charRange)
            {
                var selectValue = shapeGenerator(value, begin, end, charRange) * amount;
                foreach (var ci in lines[indices[i]])
                {
                    selection[ci] = selectValue;
                }
            }
        }

        static void SelectionBlendAdd(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = Math.Clamp(back[i] + front[i], 0.0, 1.0);
            }
        }

        static void SelectionBlendSubtract(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = Math.Clamp(back[i] - front[i], 0.0, 1.0);
            }
        }

        static void SelectionBlendMultiply(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = back[i] * front[i];
            }
        }

        static void SelectionBlendMin(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = Math.Min(back[i], front[i]);
            }
        }

        static void SelectionBlendMax(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = Math.Max(back[i], front[i]);
            }
        }

        static void SelectionBlendDifference(Span<double> back, ReadOnlySpan<double> front)
        {
            for (var i = 0; i < back.Length; i++)
            {
                back[i] = Math.Clamp(Math.Abs(back[i] - front[i]), 0.0, 1.0);
            }
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
