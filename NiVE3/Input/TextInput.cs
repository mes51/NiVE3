using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.Property;
using NiVE3.Shape;
using NiVE3.Shared.Util;
using NiVE3.Text;
using NiVE3.View.Resource;
using SixLabors.Fonts;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(TextInput), nameof(TextInput), "", "mes51", ID, "", false)]
    [InternalInput]
    class TextInput : IInput
    {
        const string ID = "0B859790-FA6E-4685-AAE2-D80F854B2B9B";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public static TextInput Instance { get; } = new TextInput();

        public string FilePath => "テキスト";

        private TextInput() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup([TextFootageSource.Instance]);
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

        const string TextBaseAnchorPointRateId = nameof(TextBaseAnchorPointRateId);

        const string TextBoxSizeId = nameof(TextBoxSizeId);

        // NOTE: 縦書きはなぜか右揃え&アルファベットが左にズレるので後回し
        // TODO: ズレる原因と回避方法の調査
        //const string TextVerticalModeId = nameof(TextVerticalModeId);

        const string TextInterCharacterBlendModeId = nameof(TextInterCharacterBlendModeId);

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

        const string TextAnimatorValueCharacterOffsetId = nameof(TextAnimatorValueCharacterOffsetId);

        const string TextAnimatorValueCharacterOffsetWhiteSpaceReplacementCharId = nameof(TextAnimatorValueCharacterOffsetWhiteSpaceReplacementCharId);

        const string TextAnimatorValueCharacterOffsetRestrictAsciiCharId = nameof(TextAnimatorValueCharacterOffsetRestrictAsciiCharId);

        const string TextAnimatorValueBlurId = nameof(TextAnimatorValueBlurId);

        public static readonly TextFootageSource Instance = new TextFootageSource();

        private TextFootageSource() { }

        public string SourceId => "text";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.Image;

        public PropertyBase[] GetOptionProperties()
        {
            return
            [
                new SourceTextProperty(SourceTextId, LanguageResourceDictionary.ResourceKeys.TextProperty_SourceText, StyledText.Empty),
                new PropertyGroup(TextMoreOptionsGroupId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextMoreOptions,
                [
                    new Vector3dProperty(TextBaseAnchorPointRateId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextMoreOptions_BaseAnchorPointRate, new Vector3d(50.0), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new Vector3dProperty(TextBoxSizeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextMoreOptions_TextBoxSize, new Vector3d(), new Vector3d(), digit: 2),
                    new EnumProperty(TextInterCharacterBlendModeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextMoreOptions_InterCharacterBlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal)
                ]),
                new AppendableProperty(TextAnimatorsId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator,
                [
                    new AppendablePropertyItem(TextAnimatorAnimatorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator, () =>
                        new PropertyGroup(TextAnimatorAnimatorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator,
                        [
                            new AppendableProperty(TextAnimatorSelectorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector,
                            [
                                new AppendablePropertyItem(TextAnimatorSelectorSelectorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Selector, () =>
                                    new PropertyGroup(TextAnimatorSelectorSelectorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Selector,
                                    [
                                        new DoubleProperty(TextAnimatorSelectorBeginId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Begin, 0.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                                        new DoubleProperty(TextAnimatorSelectorEndId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_End, 100.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                                        new DoubleProperty(TextAnimatorSelectorOffsetId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Offset, 0.0, -100.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                                        new PropertyGroup(TextAnimatorSelectorMoreOptionId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_MoreOption,
                                        [
                                            new EnumProperty(TextAnimatorSelectorCriteriaId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Criteria, typeof(SelectorCriteria), typeof(LanguageResourceDictionary), SelectorCriteria.Charactor, selectBoxWidth: 125.0),
                                            new EnumProperty(TextAnimatorSelectorBlendModeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_BlendMode, typeof(SelectorBlendMode), typeof(LanguageResourceDictionary), SelectorBlendMode.Add, selectBoxWidth: 125.0),
                                            new DoubleProperty(TextAnimatorSelectorAmountId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Amount, 100.0, 0.0, 100.0, digit: 2),
                                            new EnumProperty(TextAnimatorSelectorShapeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_Shape, typeof(SelectorShape), typeof(LanguageResourceDictionary), SelectorShape.Rectangle, selectBoxWidth: 125.0),
                                            new CheckBoxProperty(TextAnimatorSelectorEnableRandomId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_EnableRandom, false),
                                            new DoubleProperty(TextAnimatorSelectorRandomSeedId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Selector_RandomSeed, 0, double.MinValue, double.MaxValue, digit: 0)
                                        ])
                                    ]))
                            ], 0, true),
                            new AppendableProperty(TextAnimatorValueId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value,
                            [
                                new AppendablePropertyItem(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_AnchorPoint, () =>
                                    new PropertyGroup(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_AnchorPoint,
                                    [
                                        new Vector3dProperty(TextAnimatorValueAnchorPointId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_AnchorPoint, new Vector3d(), digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValuePositionId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Position, () =>
                                    new PropertyGroup(TextAnimatorValuePositionId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Position,
                                    [
                                        new Vector3dProperty(TextAnimatorValuePositionId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Position, new Vector3d(), digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueScaleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Scale, () =>
                                    new PropertyGroup(TextAnimatorValueScaleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Scale,
                                    [
                                        new Scale3dProperty(TextAnimatorValueScaleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Scale, new Vector3d(100.0), digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueAngleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Angle, () =>
                                    new PropertyGroup(TextAnimatorValueAngleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Angle,
                                    [
                                        new AngleProperty(TextAnimatorValueAngleId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Angle, 0.0, digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueSkewId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Skew, () =>
                                    new PropertyGroup(TextAnimatorValueSkewId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Skew,
                                    [
                                        new DoubleProperty(TextAnimatorValueSkewId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Skew, 0.0, -100.0, 100.0, digit: 2),
                                        new AngleProperty(TextAnimatorValueSkewAxisId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_SkewAxis, 0.0, digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueOpacityId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Opacity, () =>
                                    new PropertyGroup(TextAnimatorValueOpacityId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Opacity,
                                    [
                                        new DoubleProperty(TextAnimatorValueOpacityId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Opacity, 100.0, 0.0, 100.0, digit: 2)
                                    ])),
                                AppendablePropertyItemSeparator.Instance,
                                new AppendablePropertyItem(TextAnimatorValueFontSizeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FontSize, () =>
                                    new PropertyGroup(TextAnimatorValueFontSizeId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FontSize,
                                    [
                                        new DoubleProperty(
                                            TextAnimatorValueFontSizeId,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FontSize,
                                            0.0,
                                            double.MinValue,
                                            double.MaxValue,
                                            digit: 2,
                                            unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel
                                        )
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueFillColorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FillColor, () =>
                                    new PropertyGroup(TextAnimatorValueFillColorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FillColor,
                                    [
                                        new ColorProperty(
                                            TextAnimatorValueFillColorId,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FillColor,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FillColor,
                                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                                            Vector4.One
                                        ),
                                        new DoubleProperty(TextAnimatorValueFillColorOpacityId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_FillColorOpacity, 100.0, 0.0, 100.0, digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueTextLineColorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineColor, () =>
                                    new PropertyGroup(TextAnimatorValueTextLineColorId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineColor,
                                    [
                                        new ColorProperty(
                                            TextAnimatorValueTextLineColorId,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineColor,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineColor,
                                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                                            new Vector4(0.0F, 0.0F, 1.0F, 1.0F)
                                        ),
                                        new DoubleProperty(TextAnimatorValueTextLineColorOpacityId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity, 100.0, 0.0, 100.0, digit: 2)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineWidth, () =>
                                    new PropertyGroup(TextAnimatorValueTextLineWidthId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineWidth,
                                    [
                                        new DoubleProperty(
                                            TextAnimatorValueTextLineWidthId,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_TextLineWidth,
                                            3.0,
                                            double.MinValue,
                                            double.MaxValue,
                                            digit: 2,
                                            unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel
                                        )
                                    ])),
                                AppendablePropertyItemSeparator.Instance,
                                new AppendablePropertyItem(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_CharacterOffset, () =>
                                    new PropertyGroup(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_CharacterOffset,
                                    [
                                        new DoubleProperty(TextAnimatorValueCharacterOffsetId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_CharacterOffset, 0.0, -ushort.MaxValue, ushort.MaxValue, digit: 0),
                                        new CheckBoxProperty(TextAnimatorValueCharacterOffsetWhiteSpaceReplacementCharId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_CharacterOffset_WhiteSpaceReplacementChar, false),
                                        new CheckBoxProperty(TextAnimatorValueCharacterOffsetRestrictAsciiCharId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_CharacterOffset_RestrictAscii, false)
                                    ])),
                                new AppendablePropertyItem(TextAnimatorValueBlurId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Blur, () =>
                                    new PropertyGroup(TextAnimatorValueBlurId, LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Blur,
                                    [
                                        new Vector3dProperty(
                                            TextAnimatorValueBlurId,
                                            LanguageResourceDictionary.ResourceKeys.TextProperty_TextAnimator_Animator_Value_Blur,
                                            new Vector3d(),
                                            digit: 2,
                                            unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel,
                                            separator: ",",
                                            useLinkRatio: true
                                        )
                                    ])),
                            ], useEnableSwitch: true)
                        ]))
                ], useEnableSwitch: true)
            ];
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }

        public SourceFootageRect CalcSize(double time, int compositionWidth, int compositionHeight, PropertyValueGroup properties)
        {
            var (textOption, glyphPolygons) = BuildGlyphPolygons(properties, 1.0);
            if (textOption == null || glyphPolygons.Count < 1)
            {
                return new SourceFootageRect(Vector2d.Zero, 1, 1);
            }

            var sourceText = properties[SourceTextId] as StyledText ?? StyledText.Empty;
            var verticalMode = false; // (bool)(((PropertyValueGroup)(properties[TextMoreOptionsGroupId] ?? PropertyValueGroup.Empty))[TextVerticalModeId] ?? false);
            var (min, max, imageOrigin) = CalcTextBounds(glyphPolygons, sourceText, textOption, verticalMode);

            return new SourceFootageRect(imageOrigin, max.GetElement(2) - min.GetElement(0) + 1, max.GetElement(3) - min.GetElement(1));
        }

        public NImage ReadFrame(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            var (textOption, glyphPolygons) = BuildGlyphPolygons(properties, downSamplingRate);
            if (textOption == null || glyphPolygons.Count < 1)
            {
                return new NManagedImage(1, 1);
            }

            var sourceText = properties[SourceTextId] as StyledText ?? StyledText.Empty;
            var verticalMode = false; // (bool)(((PropertyValueGroup)(properties[TextMoreOptionsGroupId] ?? PropertyValueGroup.Empty))[TextVerticalModeId] ?? false);
            var (min, max, imageOrigin) = CalcTextBounds(glyphPolygons, sourceText, textOption, verticalMode);

            var interCharBlendMode = (BlendMode)((properties[TextMoreOptionsGroupId] as PropertyValueGroup)?[TextInterCharacterBlendModeId] ?? BlendMode.Normal);
            var image = new NManagedImage(max.GetElement(2) - min.GetElement(0) + 1, max.GetElement(3) - min.GetElement(1))
            {
                Origin = imageOrigin
            };
            foreach (var (fillPolygons, outlinePolygons, textRun, rect, blurMargin, _) in glyphPolygons)
            {
                var fillColor = textRun.FillColor;
                var textLineColor = textRun.TextLineColor;
                if (textRun.Opacity <= 0.0F || (fillColor.W <= 0.0F && (outlinePolygons.Length < 1 || textLineColor.W <= 0.0F)))
                {
                    continue;
                }

                var intLeft = rect.GetElement(0);
                var intTop = rect.GetElement(1);
                using var glyphImage = new NManagedImage(rect.GetElement(2) - intLeft + blurMargin.GetElement(0) * 2 + 1, rect.GetElement(3) - intTop + blurMargin.GetElement(1) * 2);
                var glyphLeft = intLeft - blurMargin.GetElement(0);
                var glyphTop = intTop - blurMargin.GetElement(1);
                if (imageInterpolationQuality == ImageInterpolationQuality.Level1)
                {
                    if (outlinePolygons.Length > 0 && textLineColor.W > 0.0F)
                    {
                        switch (textRun.TextLineDrawOrder)
                        {
                            case TextLineDrawOrder.AfterFill:
                                if (fillColor.W > 0.0F)
                                {
                                    ShapeRender.FillPolygonNonZeroAiliased(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop, BlendMode.Replace);
                                }
                                ShapeRender.FillPolygonNonZeroAiliased(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop);
                                break;
                            default:
                                ShapeRender.FillPolygonNonZeroAiliased(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop, BlendMode.Replace);
                                if (fillColor.W > 0.0F)
                                {
                                    ShapeRender.FillPolygonNonZeroAiliased(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop);
                                }
                                break;
                        }
                    }
                    else
                    {
                        ShapeRender.FillPolygonNonZeroAiliased(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop, BlendMode.Replace);
                    }
                }
                else
                {
                    if (outlinePolygons.Length > 0 && textLineColor.W > 0.0F)
                    {
                        switch (textRun.TextLineDrawOrder)
                        {
                            case TextLineDrawOrder.AfterFill:
                                if (fillColor.W > 0.0F)
                                {
                                    ShapeRender.FillPolygonNonZero(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop, BlendMode.Replace);
                                }
                                ShapeRender.FillPolygonNonZero(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop);
                                break;
                            default:
                                ShapeRender.FillPolygonNonZero(outlinePolygons, glyphImage, textLineColor, glyphLeft, glyphTop, BlendMode.Replace);
                                if (fillColor.W > 0.0F)
                                {
                                    ShapeRender.FillPolygonNonZero(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop);
                                }
                                break;
                        }
                    }
                    else
                    {
                        ShapeRender.FillPolygonNonZero(fillPolygons, glyphImage, fillColor, glyphLeft, glyphTop, BlendMode.Replace);
                    }
                }

                if (textRun.Blur != Vector2.Zero)
                {
                    var horizontalRange = textRun.Blur.X / 2.0F;
                    var verticalRange = textRun.Blur.Y / 2.0F;

                    Blur.BoxBlur(glyphImage, horizontalRange, verticalRange);
                    Blur.BoxBlur(glyphImage, horizontalRange, verticalRange);
                    Blur.BoxBlur(glyphImage, horizontalRange, verticalRange);
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
                else if (animator.TryGetValue<double>(TextAnimatorValueCharacterOffsetId, out var characterOffset))
                {
                    var skipReplacementChar = (bool)(animator[TextAnimatorValueCharacterOffsetWhiteSpaceReplacementCharId] ?? false);
                    var restrictAscii = (bool)(animator[TextAnimatorValueCharacterOffsetRestrictAsciiCharId] ?? false);
                    for (var i = 0; i < textRuns.Length; i++)
                    {
                        textRuns[i].CharacterOffset += (int)(characterOffset * selectedSpan[i]);
                        textRuns[i].WhiteSpaceReplacementChar = selectedSpan[i] >= 0.5 ? skipReplacementChar : textRuns[i].WhiteSpaceReplacementChar;
                        textRuns[i].RestrictAscii = selectedSpan[i] >= 0.5 ? restrictAscii : textRuns[i].RestrictAscii;
                    }
                }
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
                min = Vector128.Min(min, r);
                max = Vector128.Max(max, r);
            }

            if (Avx2.IsSupported)
            {
                return Avx2.Blend(min, max, 0b1100);
            }
            else
            {
                return (min & Vector128.Create(0, 0, -1, -1)) + (max & Vector128.Create(-1, -1, 0, 0));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DrawImage(BlendMode blendMode, NManagedImage back, NManagedImage front, float opacity, int offsetX, int offsetY)
        {
            Parallel.For(0, front.Height, y =>
            {
                var backSpan = back.GetDataSpan()[((offsetY + y) * back.Width + offsetX)..];
                var frontSpan = front.GetDataSpan()[(y * front.Width)..];

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
                            currentWord = [];
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
                currentWord = [];
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
                currentLine = [];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (TextOptions? textOption, List<BuildedTextGlyphs> glyphPolygons) BuildGlyphPolygons(PropertyValueGroup properties, double downSamplingRate)
        {
            var sourceText = properties[SourceTextId] as StyledText ?? StyledText.Empty;
            if (string.IsNullOrEmpty(sourceText.Text))
            {
                return (null, []);
            }

            var structuredExtendedTextRun = new StructuredExtendedTextRun(sourceText.Text, sourceText.DefaultStyle, sourceText.Styles);
            foreach (var animator in ((PropertyValueGroup[])(properties[TextAnimatorsId] ?? Array.Empty<PropertyValueGroup>())))
            {
                ApplyAnimator(structuredExtendedTextRun, animator);
            }
            structuredExtendedTextRun = structuredExtendedTextRun.ReconstructTextRunWithOffset();

            var moreOptions = (PropertyValueGroup)(properties[TextMoreOptionsGroupId] ?? PropertyValueGroup.Empty);
            var fontInfo = FontInfo.FindByUniqueId(sourceText.DefaultStyle.FontUniqueId) ?? FontInfo.FallbackFont;
            var font = fontInfo.FontFamily.CreateFont((float)sourceText.DefaultStyle.FontSize);
            var textOption = new TextOptions(font);
            var wrappingSize = (Vector3d)(moreOptions[TextBoxSizeId] ?? new Vector3d());
            //var verticalMode = (bool)(moreOptions[TextVerticalModeId] ?? false);
            textOption.WrappingLength = wrappingSize.X > 0.0 ? (float)wrappingSize.X : -1.0F;
            textOption.WordBreaking = wrappingSize.X > 0.0 ? WordBreaking.BreakAll : WordBreaking.Standard;
            textOption.TextRuns = structuredExtendedTextRun.Flatten();
            textOption.TextAlignment = sourceText.DefaultStyle.TextAlign switch
            {
                TextAlign.Center => TextAlignment.Center,
                TextAlign.Right => TextAlignment.End,
                _ => TextAlignment.Start,
            };
            //textOption.LayoutMode = verticalMode ? LayoutMode.VerticalMixedRightLeft : LayoutMode.HorizontalTopBottom;
            var baseAnchorPointRate = (Vector2)(Vector3d)(moreOptions[TextBaseAnchorPointRateId] ?? new Vector3d(50.0)) * 0.01F;
            var glyphBuilder = new StyledGlyphBuilder((float)wrappingSize.X, (float)wrappingSize.Y, downSamplingRate, baseAnchorPointRate);
            TextRenderer.RenderTextTo(glyphBuilder, structuredExtendedTextRun.SourceText, textOption);
            var glyphPolygons = new List<BuildedTextGlyphs>();
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
                glyphPolygons.Add(new BuildedTextGlyphs(fillPolygons, outlinePolygons, glyph.TextRun, rect, blurMargin, origin));
            }

            return (textOption, glyphPolygons);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector128<int> min, Vector128<int> max, Vector2d origin) CalcTextBounds(List<BuildedTextGlyphs> glyphPolygons, StyledText sourceText, TextOptions textOption, bool verticalMode)
        {
            var min = Vector128.Create(int.MaxValue);
            var max = Vector128.Create(int.MinValue);
            foreach (var (_, outlinePolygons, textRun, r, blurMargin, _) in glyphPolygons)
            {
                min = Vector128.Min(min, r - blurMargin);
                max = Vector128.Max(max, r + blurMargin);
            }

            var imageOrigin = (Vector2d)glyphPolygons[0].Origin + new Vector2d(glyphPolygons[0].Rect.GetElement(0) - min.GetElement(0), glyphPolygons[0].Rect.GetElement(1) - min.GetElement(1));
            var measure = TextMeasurer.MeasureBounds(sourceText.Text, textOption);
            imageOrigin += (verticalMode, sourceText.DefaultStyle.TextAlign) switch
            {
                (false, TextAlign.Center) => new Vector2d(measure.Width * 0.5, 0.0),
                (false, TextAlign.Right) => new Vector2d(measure.Width, 0.0),
                (true, TextAlign.Center) => new Vector2d(0.0, measure.Height * 0.5),
                (true, TextAlign.Right) => new Vector2d(0.0, measure.Height),
                _ => Vector2d.Zero
            };

            return (min, max, imageOrigin);
        }
    }

    record BuildedTextGlyphs(Polygon[] FillPolygons, Polygon[] OutlinePolygons, ExtendedTextRun TextRun, Vector128<int> Rect, Vector128<int> BlurMargin, Vector2 Origin);

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
