using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Shape;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using Polygon = NiVE3.Shape.Polygon;
using Brush = NiVE3.Shape.Brush;
using SolidBrush = NiVE3.Shape.SolidBrush;
using LinearGradientBrush = NiVE3.Shape.LinearGradientBrush;
using RadialGradientBrush = NiVE3.Shape.RadialGradientBrush;
using NiVE3.Plugin.ValueObject;
using System.Windows.Media;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(ShapeInput), nameof(TextInput), "", "mes51", ID, "", false)]
    [InternalInput]
    class ShapeInput : IInput
    {
        const string ID = "555C4337-228E-48F6-A310-66F216FCAC7D";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public static ShapeInput Instance { get; } = new ShapeInput();

        public string FilePath => "シェイプ";

        private ShapeInput() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup([ShapeFootageSource.Instance]);
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }

    class ShapeFootageSource : ICustomizableFootageSource
    {
        const string ContentPropertyId = nameof(ContentPropertyId);

        const string GroupPropertyId = nameof(GroupPropertyId);

        const string GroupContentPropertyId = nameof(GroupContentPropertyId);

        const string GroupTransformGroupId = nameof(GroupTransformGroupId);

        const string GroupTransformAnchorPointId = nameof(GroupTransformAnchorPointId);

        const string GroupTransformPositionId = nameof(GroupTransformPositionId);

        const string GroupTransformScaleId = nameof(GroupTransformScaleId);

        const string GroupTransformAngleId = nameof(GroupTransformAngleId);

        const string GroupTransformSkewId = nameof(GroupTransformSkewId);

        const string GroupTransformSkewAxisId = nameof(GroupTransformSkewAxisId);

        const string GroupTransformOpacityId = nameof(GroupTransformOpacityId);

        const string RectangleGroupId = nameof(RectangleGroupId);

        const string RectangleSizeId = nameof(RectangleSizeId);

        const string RectanglePositionId = nameof(RectanglePositionId);

        const string RectangleCornerRoundedId = nameof(RectangleCornerRoundedId);

        const string CircleGroupId = nameof(CircleGroupId);

        const string CircleSizeId = nameof(CircleSizeId);

        const string CirclePositionId = nameof(CirclePositionId);

        const string RegularPolygonGroupId = nameof(RegularPolygonGroupId);

        const string RegularPolygonPointCountId = nameof(RegularPolygonPointCountId);

        const string RegularPolygonRadiusId = nameof(RegularPolygonRadiusId);

        const string RegularPolygonRoundedId = nameof(RegularPolygonRoundedId);

        const string RegularPolygonPositionId = nameof(RegularPolygonPositionId);

        const string RegularPolygonAngleId = nameof(RegularPolygonAngleId);

        const string StarGroupId = nameof(StarGroupId);

        const string StarPointCountId = nameof(StarPointCountId);

        const string StarOuterRadiusId = nameof(StarOuterRadiusId);

        const string StarInnerRadiusId = nameof(StarInnerRadiusId);

        const string StarOuterRoundedId = nameof(StarOuterRoundedId);

        const string StarInnerRoundedId = nameof(StarInnerRoundedId);

        const string StarPositionId = nameof(StarPositionId);

        const string StarAngleId = nameof(StarAngleId);

        const string SolidFillGroupId = nameof(SolidFillGroupId);

        const string SolidFillRuleId = nameof(SolidFillRuleId);

        const string SolidFillColorId = nameof(SolidFillColorId);

        const string SolidFillOpacityId = nameof(SolidFillOpacityId);

        const string SolidFillBlendModeId = nameof(SolidFillBlendModeId);

        const string GradientFillGroupId = nameof(GradientFillGroupId);

        const string GradientFillRuleId = nameof(GradientFillRuleId);

        const string GradientFillTypeId = nameof(GradientFillTypeId);

        const string GradientFillColorId = nameof(GradientFillColorId);

        const string GradientFillUseOkLabInterpolationId = nameof(GradientFillUseOkLabInterpolationId);

        const string GradientFillBeginPositionId = nameof(GradientFillBeginPositionId);

        const string GradientFillEndPositionId = nameof(GradientFillEndPositionId);

        const string GradientFillOpacityId = nameof(GradientFillOpacityId);

        const string GradientFillBlendModeId = nameof(GradientFillBlendModeId);

        const string SolidStrokeGroupId = nameof(SolidStrokeGroupId);

        const string SolidStrokeColorId = nameof(SolidStrokeColorId);

        const string SolidStrokeOpacityId = nameof(SolidStrokeOpacityId);

        const string SolidStrokeWidthId = nameof(SolidStrokeWidthId);

        const string SolidStrokeEndCapStyleTypeId = nameof(SolidStrokeEndCapStyleTypeId);

        const string SolidStrokeJoinStyleTypeId = nameof(SolidStrokeJoinStyleTypeId);

        const string SolidStrokeBlendModeId = nameof(SolidStrokeBlendModeId);

        const string GradientStrokeGroupId = nameof(GradientStrokeGroupId);

        const string GradientStrokeRuleId = nameof(GradientStrokeRuleId);

        const string GradientStrokeTypeId = nameof(GradientStrokeTypeId);

        const string GradientStrokeColorId = nameof(GradientStrokeColorId);

        const string GradientStrokeUseOkLabInterpolationId = nameof(GradientStrokeUseOkLabInterpolationId);

        const string GradientStrokeBeginPositionId = nameof(GradientStrokeBeginPositionId);

        const string GradientStrokeEndPositionId = nameof(GradientStrokeEndPositionId);

        const string GradientStrokeOpacityId = nameof(GradientStrokeOpacityId);

        const string GradientStrokeWidthId = nameof(GradientStrokeWidthId);

        const string GradientStrokeEndCapStyleTypeId = nameof(GradientStrokeEndCapStyleTypeId);

        const string GradientStrokeJoinStyleTypeId = nameof(GradientStrokeJoinStyleTypeId);

        const string GradientStrokeBlendModeId = nameof(GradientStrokeBlendModeId);

        const string RepeaterGroupId = nameof(RepeaterGroupId);

        const string RepeaterCountId = nameof(RepeaterCountId);

        const string RepeaterOffsetId = nameof(RepeaterOffsetId);

        const string RepeaterTransformGroupId = nameof(RepeaterTransformGroupId);

        const string RepeaterTransformAnchorPointId = nameof(RepeaterTransformAnchorPointId);

        const string RepeaterTransformPositionId = nameof(RepeaterTransformPositionId);

        const string RepeaterTransformScaleId = nameof(RepeaterTransformScaleId);

        const string RepeaterTransformAngleId = nameof(RepeaterTransformAngleId);

        const string RepeaterTransformBeginPointOpacityId = nameof(RepeaterTransformBeginPointOpacityId);

        const string RepeaterTransformEndPointOpacityId = nameof(RepeaterTransformEndPointOpacityId);

        const string CombineGroupId = nameof(CombineGroupId);

        const string CombineTypeId = nameof(CombineTypeId);

        const string TrimmingGroupId = nameof(TrimmingGroupId);

        const string TrimmingBeginId = nameof(TrimmingBeginId);

        const string TrimmingEndId = nameof(TrimmingEndId);

        const string TrimmingOffsetId = nameof(TrimmingOffsetId);

        public static ShapeFootageSource Instance { get; } = new ShapeFootageSource();

        public string SourceId => "shape";

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.Image;

        public PropertyBase[] GetOptionProperties()
        {
            AppendablePropertyItem[] groupItems;
            groupItems =
            [
                new AppendablePropertyItem("Placeholder", "Placeholder", () => new PropertyGroup("Placeholder", "Placeholder", [])),
                AppendablePropertyItemSeparator.Instance,
                new AppendablePropertyItem(RectangleGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RectangleGroup, () =>
                    new PropertyGroup(RectangleGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RectangleGroup,
                    [
                        new Vector3dProperty(RectangleSizeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Size, new Vector3d(100.0), Vector3d.Zero, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel, separator: ",", useLinkRatio: true),
                        new Vector3dProperty(RectanglePositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Position, new Vector3d(), digit: 2),
                        new DoubleProperty(RectangleCornerRoundedId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RectangleGroup_CornerRounded, 0.0, 0.0, double.MaxValue, digit: 2)
                    ])),
                new AppendablePropertyItem(CircleGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_CircleGroup, () =>
                    new PropertyGroup(CircleGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_CircleGroup,
                    [
                        new Vector3dProperty(CircleSizeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Size, new Vector3d(100.0), Vector3d.Zero, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel, separator: ",", useLinkRatio: true),
                        new Vector3dProperty(CirclePositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Position, new Vector3d(), digit: 2)
                    ])),
                new AppendablePropertyItem(RegularPolygonGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RegularPolygonGroup, () =>
                    new PropertyGroup(RegularPolygonGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RegularPolygonGroup,
                    [
                        new DoubleProperty(RegularPolygonPointCountId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_PolygonGroup_Points, 5.0, 3.0, 10000.0, digit: 0),
                        new DoubleProperty(RegularPolygonRadiusId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RegularPolygonGroup_Radius, 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(RegularPolygonRoundedId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RegularPolygonGroup_Rounded, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new Vector3dProperty(RegularPolygonPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Position, Vector3d.Zero, digit: 2),
                        new AngleProperty(RegularPolygonAngleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Angle, 0.0, digit: 2)
                    ])),
                new AppendablePropertyItem(StarGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup, () =>
                    new PropertyGroup(StarGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup,
                    [
                        new DoubleProperty(StarPointCountId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_PolygonGroup_Points, 5.0, 3.0, 10000.0, digit: 0),
                        new DoubleProperty(StarOuterRadiusId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup_OuterRadius, 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(StarInnerRadiusId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup_InnerRadius, 50.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(StarOuterRoundedId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup_OuterRounded, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(StarInnerRoundedId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StarGroup_InnerRounded, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new Vector3dProperty(StarPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Position, Vector3d.Zero, digit: 2),
                        new AngleProperty(StarAngleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_ShapeObjectGroup_Angle, 0.0, digit: 2)
                    ])),
                AppendablePropertyItemSeparator.Instance,
                new AppendablePropertyItem(SolidFillGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_SolidFillGroup, () =>
                    new PropertyGroup(SolidFillGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_SolidFillGroup,
                    [
                        new EnumProperty(SolidFillRuleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_FillGroup_FillRule, typeof(ShapeFillRule), typeof(LanguageResourceDictionary), ShapeFillRule.NonZero, selectBoxWidth: 100.0),
                        new ColorProperty(
                            SolidFillColorId,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                            Vector4.One
                        ),
                        new DoubleProperty(SolidFillOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(SolidFillBlendModeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 100.0)
                    ])),
                new AppendablePropertyItem(GradientFillGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientFillGroup, () =>
                    new PropertyGroup(GradientFillGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientFillGroup,
                    [
                        new EnumProperty(GradientFillRuleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_FillGroup_FillRule, typeof(ShapeFillRule), typeof(LanguageResourceDictionary), ShapeFillRule.NonZero, selectBoxWidth: 100.0),
                        new EnumProperty(GradientFillTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_Type, typeof(GradientType), typeof(LanguageResourceDictionary), GradientType.Linear, selectBoxWidth: 100.0),
                        new Vector3dProperty(GradientFillBeginPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_BeginPosition, Vector3d.Zero, digit: 2),
                        new Vector3dProperty(GradientFillEndPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_EndPosition, new Vector3d(100.0, 0.0, 0.0), digit: 2),
                        new ColorGradientProperty(
                            GradientFillColorId,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_Color_Edit,
                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                            showPreviewOKLabInterpolation: true
                        ),
                        new CheckBoxProperty(GradientFillUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_UseOkLabInterpolation, false),
                        new DoubleProperty(GradientFillOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(GradientFillBlendModeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 100.0)
                    ])),
                new AppendablePropertyItem(SolidStrokeGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_SolidStrokeGroup, () =>
                    new PropertyGroup(SolidStrokeGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_SolidStrokeGroup,
                    [
                        new ColorProperty(
                            SolidStrokeColorId,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                            new Vector4(0.0F, 0.0F, 1.0F, 1.0F)
                        ),
                        new DoubleProperty(SolidStrokeOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(SolidStrokeWidthId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_Width, 4.0, 0.0, double.MaxValue, digit: 2),
                        new EnumProperty(SolidStrokeEndCapStyleTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_EndCapStyleType, typeof(EndCapStyle), typeof(LanguageResourceDictionary), EndCapStyle.Butt, selectBoxWidth: 100.0),
                        new EnumProperty(SolidStrokeJoinStyleTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_JoinStyleType, typeof(JointStyle), typeof(LanguageResourceDictionary), JointStyle.Square, selectBoxWidth: 100.0),
                        new EnumProperty(SolidStrokeBlendModeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 100.0)
                    ])),
                new AppendablePropertyItem(GradientStrokeGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientStrokeGroup, () =>
                    new PropertyGroup(GradientStrokeGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientStrokeGroup,
                    [
                        new EnumProperty(GradientStrokeTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_Type, typeof(GradientType), typeof(LanguageResourceDictionary), GradientType.Linear, selectBoxWidth: 100.0),
                        new Vector3dProperty(GradientStrokeBeginPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_BeginPosition, Vector3d.Zero, digit: 2),
                        new Vector3dProperty(GradientStrokeEndPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_EndPosition, new Vector3d(100.0, 0.0, 0.0), digit: 2),
                        new ColorGradientProperty(
                            GradientStrokeColorId,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Color,
                            LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_Color_Edit,
                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                            showPreviewOKLabInterpolation: true
                        ),
                        new CheckBoxProperty(GradientStrokeUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_GradientGroup_UseOkLabInterpolation, false),
                        new DoubleProperty(GradientStrokeOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(GradientStrokeWidthId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_Width, 4.0, 0.0, double.MaxValue, digit: 2),
                        new EnumProperty(GradientStrokeEndCapStyleTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_EndCapStyleType, typeof(EndCapStyle), typeof(LanguageResourceDictionary), EndCapStyle.Butt, selectBoxWidth: 100.0),
                        new EnumProperty(GradientStrokeJoinStyleTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_StrokeGroup_JoinStyleType, typeof(JointStyle), typeof(LanguageResourceDictionary), JointStyle.Square, selectBoxWidth: 100.0),
                        new EnumProperty(GradientStrokeBlendModeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Drawing_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 100.0)
                    ])),
                AppendablePropertyItemSeparator.Instance,
                new AppendablePropertyItem(RepeaterGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup, () =>
                    new PropertyGroup(RepeaterGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup,
                    [
                        new DoubleProperty(RepeaterCountId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup_Count, 3.0, 0.0, double.MaxValue, digit: 1),
                        new DoubleProperty(RepeaterOffsetId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup_Offset, 0.0, double.MinValue, double.MaxValue, digit: 1),
                        new PropertyGroup(RepeaterTransformGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform,
                        [
                            new Vector3dProperty(RepeaterTransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_AnchorPoint, Vector3d.Zero, digit: 2),
                            new Vector3dProperty(RepeaterTransformPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Position, new Vector3d(100.0, 0.0, 0.0), digit: 2),
                            new Scale3dProperty(RepeaterTransformScaleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Scale, new Vector3d(100.0), digit: 2),
                            new AngleProperty(RepeaterTransformAngleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Angle, 0.0, digit: 2),
                            new DoubleProperty(RepeaterTransformBeginPointOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new DoubleProperty(RepeaterTransformEndPointOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_RepeaterGroup_Transform_EndPointOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                        ])
                    ])),
                new AppendablePropertyItem(CombineGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_CombineGroup, () =>
                    new PropertyGroup(CombineGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_CombineGroup,
                    [
                        new EnumProperty(CombineTypeId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_CombineGroup_CombineType, typeof(ClippingOperation), typeof(LanguageResourceDictionary), ClippingOperation.Union, selectBoxWidth: 100)
                    ])),
                new AppendablePropertyItem(TrimmingGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_TrimmingGroup, () =>
                    new PropertyGroup(TrimmingGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_TrimmingGroup,
                    [
                        new DoubleProperty(TrimmingBeginId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_TrimmingGroup_Begin, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(TrimmingEndId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_TrimmingGroup_End, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new AngleProperty(TrimmingOffsetId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_TrimmingGroup_Offset, 0.0, digit: 2)
                    ]))
            ];
            groupItems[0] = new AppendablePropertyItem(GroupPropertyId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Group, () =>
                new PropertyGroup(GroupPropertyId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Group,
                [
                    new AppendableProperty(GroupContentPropertyId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Group_Content, groupItems, useEnableSwitch: true),
                    new PropertyGroup(GroupTransformGroupId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform,
                    [
                        new Vector3dProperty(GroupTransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_AnchorPoint, Vector3d.Zero, digit: 2),
                        new Vector3dProperty(GroupTransformPositionId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Position, Vector3d.Zero, digit: 2),
                        new Scale3dProperty(GroupTransformScaleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Scale, new Vector3d(100.0), digit: 2),
                        new DoubleProperty(GroupTransformSkewId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Skew, 0.0, -100.0, 100.0, digit: 2),
                        new AngleProperty(GroupTransformSkewAxisId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_SkewAxis, 0.0, digit: 2),
                        new AngleProperty(GroupTransformAngleId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Angle, 0.0, digit: 2),
                        new DoubleProperty(GroupTransformOpacityId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Transform_Opacity, 100.0, 0.0, 100.0, digit: 2)
                    ])
                ]));
            return
            [
                new AppendableProperty(ContentPropertyId, LanguageResourceDictionary.ResourceKeys.ShapeProperty_Content, groupItems, useEnableSwitch: true)
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
            var contents = (properties[ContentPropertyId] as PropertyValueGroup[]) ?? [];
            var tree = CreateShapeTree(contents);
            var drawable = tree.GetDrawables(1.0F).ToArray();

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach (var (_, _, _, p) in drawable)
            {
                var pathBounds = p.Bounds;
                minX = Math.Min(minX, (int)MathF.Floor(pathBounds.Left));
                minY = Math.Min(minY, (int)MathF.Floor(pathBounds.Top));
                maxX = Math.Max(maxX, (int)MathF.Ceiling(pathBounds.Right));
                maxY = Math.Max(maxY, (int)MathF.Ceiling(pathBounds.Bottom));
            }

            return new SourceFootageRect(-new Vector2d(minX, minY), maxX - minX, maxY - minY);
        }

        public NImage ReadFrame(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            var contents = (properties[ContentPropertyId] as PropertyValueGroup[]) ?? [];
            var tree = CreateShapeTree(contents);
            var drawable = tree.GetDrawables((float)(1.0 / downSamplingRate)).ToArray();

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach (var (_, _, _, p) in drawable)
            {
                var pathBounds = p.Bounds;
                minX = Math.Min(minX, (int)MathF.Floor(pathBounds.Left));
                minY = Math.Min(minY, (int)MathF.Floor(pathBounds.Top));
                maxX = Math.Max(maxX, (int)MathF.Ceiling(pathBounds.Right));
                maxY = Math.Max(maxY, (int)MathF.Ceiling(pathBounds.Bottom));
            }

            var image = new NManagedImage(maxX - minX + 1, maxY - minY + 1)
            {
                Origin = -new Vector2d(minX, minY)
            };

            foreach (var (brush, fillRule, blendMode, path) in drawable)
            {
                var bounds = path.Bounds;
                var left = (int)MathF.Floor(bounds.Left);
                var top = (int)MathF.Floor(bounds.Top);
                switch (fillRule, imageInterpolationQuality)
                {
                    case (ShapeFillRule.NonZero, ImageInterpolationQuality.Level1):
                        ShapeRender.FillPolygonNonZeroAiliased(ToPolygons(path), image, brush, minX, minY, blendMode);
                        break;
                    case (ShapeFillRule.NonZero, _):
                        ShapeRender.FillPolygonNonZero(ToPolygons(path), image, brush, minX, minY, blendMode);
                        break;
                    case (ShapeFillRule.EvenOdd, ImageInterpolationQuality.Level1):
                        ShapeRender.FillPolygonEvenOddAiliased(ToPolygons(path), image, brush, minX, minY, blendMode);
                        break;
                    case (ShapeFillRule.EvenOdd, _):
                        ShapeRender.FillPolygonEvenOdd(ToPolygons(path), image, brush, minX, minY, blendMode);
                        break;
                }
            }

            return image;
        }

        static ShapeGroupTree CreateShapeTree(PropertyValueGroup[] properties)
        {
            var tree = new ShapeGroupTree();

            foreach (var property in properties)
            {
                if (property.TryGetValue(GroupContentPropertyId, out var group) && group is PropertyValueGroup[] groupContents)
                {
                    var chilld = CreateShapeTree(groupContents);

                    var transformGroup = (PropertyValueGroup)(property[GroupTransformGroupId] ?? PropertyValueGroup.Empty);
                    var anchorPoint = (Vector3d)(transformGroup[GroupTransformAnchorPointId] ?? Vector3d.Zero);
                    var position = (Vector3d)(transformGroup[GroupTransformPositionId] ?? Vector3d.Zero);
                    var scale = (Vector3d)(transformGroup[GroupTransformScaleId] ?? Vector3d.Zero);
                    var skew = (double)(transformGroup[GroupTransformSkewId] ?? 0.0);
                    var skewAxis = (double)(transformGroup[GroupTransformSkewAxisId] ?? 0.0);
                    var angle = (double)(transformGroup[GroupTransformAngleId] ?? 0.0);
                    var opacity = (double)(transformGroup[GroupTransformOpacityId] ?? 0.0);
                    chilld.Transform(anchorPoint, position, scale * 0.01, skew * 0.01, skewAxis, angle, opacity * 0.01);

                    tree.AddNode(chilld);
                }
                else if (property.TryGetValue(RectangleSizeId, out var rectSize))
                {
                    var position = (Vector3)(Vector3d)(property[RectanglePositionId] ?? Vector3d.Zero);
                    var size = (Vector3)(Vector3d)(rectSize ?? Vector3d.Zero);
                    if (size.X <= 0.0F || size.Y <= 0.0F)
                    {
                        continue;
                    }

                    position -= size * 0.5F;
                    var cornerRounded = Math.Min((float)(double)(property[RectangleCornerRoundedId] ?? 0.0), Math.Min(size.X, size.Y) * 0.5F);
                    if (cornerRounded <= 0.0F)
                    {
                        tree.AddNode(new ShapePath(new RectangularPolygon(position.X, position.Y, size.X, size.Y)));
                    }
                    else
                    {
                        var cornerRoundedRadius = cornerRounded * 0.5F;
                        var rightBottom = position + size;
                        var pathBuilder = new PathBuilder();

                        pathBuilder.StartFigure();
                        pathBuilder.MoveTo(new PointF(position.X + cornerRounded, position.Y));
                        pathBuilder.LineTo(rightBottom.X - cornerRounded, position.Y);
                        pathBuilder.AddArc(new PointF(rightBottom.X - cornerRounded, position.Y), cornerRounded, cornerRounded, 90.0F, false, true, new PointF(rightBottom.X, position.Y + cornerRounded));
                        pathBuilder.LineTo(rightBottom.X, rightBottom.Y - cornerRounded);
                        pathBuilder.AddArc(new PointF(rightBottom.X, rightBottom.Y - cornerRounded), cornerRounded, cornerRounded, 90.0F, false, true, new PointF(rightBottom.X - cornerRounded, rightBottom.Y));
                        pathBuilder.LineTo(position.X + cornerRounded, rightBottom.Y);
                        pathBuilder.AddArc(new PointF(position.X + cornerRounded, rightBottom.Y), cornerRounded, cornerRounded, 90.0F, false, true, new PointF(position.X, rightBottom.Y - cornerRounded));
                        pathBuilder.LineTo(position.X, position.Y + cornerRounded);
                        pathBuilder.AddArc(new PointF(position.X, position.Y + cornerRounded), cornerRounded, cornerRounded, 90.0F, false, true, new PointF(position.X + cornerRounded, position.Y));
                        pathBuilder.CloseFigure();

                        tree.AddNode(new ShapePath(pathBuilder.Build()));
                    }
                }
                else if (property.TryGetValue(CircleSizeId, out var circleSize))
                {
                    var position = (Vector3)(Vector3d)(property[CirclePositionId] ?? Vector3d.Zero);
                    var size = (Vector3)(Vector3d)(circleSize ?? Vector3d.Zero);

                    if (size.X > 0.0F && size.Y > 0.0F)
                    {
                        tree.AddNode(new ShapePath(new EllipsePolygon(position.X, position.Y, size.X, size.Y)));
                    }
                }
                else if (property.TryGetValue(RegularPolygonPointCountId, out var regularPolygonPointCount))
                {
                    var count = (int)(double)(regularPolygonPointCount ?? 0.0);
                    var radius = (float)(double)(property[RegularPolygonRadiusId] ?? 0.0);
                    if (radius <= 0.0F)
                    {
                        continue;
                    }

                    var rounded = (float)(double)(property[RegularPolygonRoundedId] ?? 0.0);
                    var position = (Vector3)(Vector3d)(property[RegularPolygonPositionId] ?? Vector3d.Zero);
                    var angle = (float)(double)(property[RegularPolygonAngleId] ?? 0.0);

                    var pointRad = Math.PI * 2.0 / count;
                    var k = (4.0 / 3.0) * Math.Tan(Math.PI / (count * 2)) * rounded * 0.01 * radius;
                    var pathBuilder = new PathBuilder();
                    pathBuilder.StartFigure();
                    pathBuilder.MoveTo(new PointF(0.0F, -radius));
                    if (rounded == 0.0)
                    {
                        for (var i = 1; i < count; i++)
                        {
                            var rad = pointRad * i;
                            pathBuilder.LineTo((float)Math.Sin(rad) * radius, (float)-Math.Cos(rad) * radius);
                        }
                    }
                    else
                    {
                        for (var i = 1; i <= count; i++)
                        {
                            var prevRad = pointRad * (i - 1);
                            var rad = pointRad * i;
                            var sp = new PointF((float)Math.Sin(prevRad) * radius, (float)-Math.Cos(prevRad) * radius);
                            var ep = new PointF((float)Math.Sin(rad) * radius, (float)-Math.Cos(rad) * radius);
                            var c1 = sp + new PointF((float)(Math.Sin(prevRad + Math.PI * 0.5) * k), (float)(-Math.Cos(prevRad + Math.PI * 0.5) * k));
                            var c2 = ep + new PointF((float)(Math.Sin(rad - Math.PI * 0.5) * k), (float)(-Math.Cos(rad - Math.PI * 0.5) * k));
                            pathBuilder.AddCubicBezier(sp, c1, c2, ep);
                        }
                    }
                    pathBuilder.CloseFigure();

                    var transform = Matrix3x2.CreateRotation(angle / 180.0F * MathF.PI) * Matrix3x2.CreateTranslation(position.X, position.Y);
                    tree.AddNode(new ShapePath(pathBuilder.Build().Transform(transform)));
                }
                else if (property.TryGetValue(StarPointCountId, out var starPointCount))
                {
                    var count = (int)(double)(starPointCount ?? 0.0) * 2;
                    var outerRadius = (float)(double)(property[StarOuterRadiusId] ?? 0.0);
                    var innerRadius = (float)(double)(property[StarInnerRadiusId] ?? 0.0);
                    if (outerRadius <= 0.0F && innerRadius <= 0.0F)
                    {
                        continue;
                    }

                    var outerRounded = (float)(double)(property[StarOuterRoundedId] ?? 0.0);
                    var innerRounded = (float)(double)(property[StarInnerRoundedId] ?? 0.0);
                    var position = (Vector3)(Vector3d)(property[StarPositionId] ?? Vector3d.Zero);
                    var angle = (float)(double)(property[StarAngleId] ?? 0.0);

                    var pointRad = Math.PI * 2.0 / count;
                    var ok = (4.0 / 3.0) * Math.Tan(Math.PI / (count * 2)) * outerRounded * 0.01 * outerRadius;
                    var ik = (4.0 / 3.0) * Math.Tan(Math.PI / (count * 2)) * innerRounded * 0.01 * innerRadius;
                    var pathBuilder = new PathBuilder();
                    pathBuilder.StartFigure();
                    if (outerRounded == 0.0 && innerRounded == 0.0)
                    {
                        for (var i = 0; i < count; i += 2)
                        {
                            var oRad = pointRad * i;
                            var iRad = pointRad * (i + 1);
                            if (i == 0)
                            {
                                pathBuilder.MoveTo(new PointF((float)Math.Sin(oRad) * outerRadius, (float)-Math.Cos(oRad) * outerRadius));
                            }
                            else
                            {
                                pathBuilder.LineTo((float)Math.Sin(oRad) * outerRadius, (float)-Math.Cos(oRad) * outerRadius);
                            }
                            pathBuilder.LineTo((float)Math.Sin(iRad) * innerRadius, (float)-Math.Cos(iRad) * innerRadius);
                        }
                    }
                    else
                    {
                        pathBuilder.MoveTo(new PointF(0.0F, -outerRadius));
                        for (var i = 1; i < count; i += 2)
                        {
                            var prevOuterRad = pointRad * (i - 1);
                            var innerRad = pointRad * i;
                            var outerRad = pointRad * (i + 1);
                            var pop = new PointF((float)Math.Sin(prevOuterRad) * outerRadius, (float)-Math.Cos(prevOuterRad) * outerRadius);
                            var ip = new PointF((float)Math.Sin(innerRad) * innerRadius, (float)-Math.Cos(innerRad) * innerRadius);
                            var op = new PointF((float)Math.Sin(outerRad) * outerRadius, (float)-Math.Cos(outerRad) * outerRadius);

                            var ic1 = pop + new PointF((float)(Math.Sin(prevOuterRad + Math.PI * 0.5) * ok), (float)(-Math.Cos(prevOuterRad + Math.PI * 0.5) * ok));
                            var ic2 = ip + new PointF((float)(Math.Sin(innerRad - Math.PI * 0.5) * ik), (float)(-Math.Cos(innerRad - Math.PI * 0.5) * ik));
                            var oc1 = ip + new PointF((float)(Math.Sin(innerRad + Math.PI * 0.5) * ik), (float)(-Math.Cos(innerRad + Math.PI * 0.5) * ik));
                            var oc2 = op + new PointF((float)(Math.Sin(outerRad - Math.PI * 0.5) * ok), (float)(-Math.Cos(outerRad - Math.PI * 0.5) * ok));

                            pathBuilder.AddCubicBezier(pop, ic1, ic2, ip);
                            pathBuilder.AddCubicBezier(ip, oc1, oc2, op);
                        }
                    }
                    pathBuilder.CloseFigure();

                    var transform = Matrix3x2.CreateRotation(angle / 180.0F * MathF.PI) * Matrix3x2.CreateTranslation(position.X, position.Y);
                    tree.AddNode(new ShapePath(pathBuilder.Build().Transform(transform)));
                }
                else if (property.TryGetValue(SolidFillColorId, out var solifFillColor))
                {
                    var color = (Vector4)(solifFillColor ?? Vector4.Zero);
                    var opacity = (float)(double)(property[SolidFillOpacityId] ?? 0.0) * 0.01F;
                    var fillRule = (ShapeFillRule)(property[SolidFillRuleId] ?? ShapeFillRule.NonZero);
                    var blendModel = (BlendMode)(property[SolidFillBlendModeId] ?? BlendMode.Normal);

                    tree.AddNode(new ShapeSolidFill(color, fillRule, blendModel) { Opacity = opacity });
                }
                else if (property.TryGetValue(GradientFillColorId, out var gradientFillColor))
                {
                    var color = (ColorGradient)(gradientFillColor ?? ColorGradient.Empty);
                    var useOkLabInterpolation = (bool)(property[GradientFillUseOkLabInterpolationId] ?? false);
                    var type = (GradientType)(property[GradientFillTypeId] ?? GradientType.Linear);
                    var begin = (Vector3)(Vector3d)(property[GradientFillBeginPositionId] ?? Vector3d.Zero);
                    var end = (Vector3)(Vector3d)(property[GradientFillEndPositionId] ?? Vector3d.Zero);
                    var opacity = (float)(double)(property[GradientFillOpacityId] ?? 0.0);
                    var fillRule = (ShapeFillRule)(property[GradientFillRuleId] ?? ShapeFillRule.NonZero);
                    var blendModel = (BlendMode)(property[GradientFillBlendModeId] ?? BlendMode.Normal);

                    tree.AddNode(new ShapeGradientFill(color, useOkLabInterpolation, type, begin.AsVector2(), end.AsVector2(), fillRule, blendModel));
                }
                else if (property.TryGetValue(SolidStrokeColorId, out var solifStrokeColor))
                {
                    var color = (Vector4)(solifStrokeColor ?? Vector4.Zero);
                    var opacity = (float)(double)(property[SolidStrokeOpacityId] ?? 0.0) * 0.01F;
                    var width = (float)(double)(property[SolidStrokeWidthId] ?? ShapeFillRule.NonZero);
                    var endCapStyle = (EndCapStyle)(property[SolidStrokeEndCapStyleTypeId] ?? EndCapStyle.Butt);
                    var joinStyle = (JointStyle)(property[SolidStrokeJoinStyleTypeId] ?? JointStyle.Square);
                    var blendModel = (BlendMode)(property[SolidStrokeBlendModeId] ?? BlendMode.Normal);

                    tree.AddNode(new ShapeSolidStroke(color, width, endCapStyle, joinStyle, blendModel) { Opacity = opacity });
                }
                else if (property.TryGetValue(GradientStrokeColorId, out var gradientStrokeColor))
                {
                    var color = (ColorGradient)(gradientStrokeColor ?? ColorGradient.Empty);
                    var useOkLabInterpolation = (bool)(property[GradientStrokeUseOkLabInterpolationId] ?? false);
                    var type = (GradientType)(property[GradientStrokeTypeId] ?? GradientType.Linear);
                    var begin = (Vector3)(Vector3d)(property[GradientStrokeBeginPositionId] ?? Vector3d.Zero);
                    var end = (Vector3)(Vector3d)(property[GradientStrokeEndPositionId] ?? Vector3d.Zero);
                    var opacity = (float)(double)(property[GradientStrokeOpacityId] ?? 0.0) * 0.01F;
                    var width = (float)(double)(property[GradientStrokeWidthId] ?? ShapeFillRule.NonZero);
                    var endCapStyle = (EndCapStyle)(property[GradientStrokeEndCapStyleTypeId] ?? EndCapStyle.Butt);
                    var joinStyle = (JointStyle)(property[GradientStrokeJoinStyleTypeId] ?? JointStyle.Square);
                    var blendModel = (BlendMode)(property[GradientStrokeBlendModeId] ?? BlendMode.Normal);

                    tree.AddNode(new ShapeGradientStroke(color, useOkLabInterpolation, type, begin.AsVector2(), end.AsVector2(), width, endCapStyle, joinStyle, blendModel) { Opacity = opacity });
                }
                else if (property.TryGetValue(RepeaterCountId, out var repeaterCount))
                {
                    var count = (int)(double)(repeaterCount ?? 0.0);
                    var offset = (double)(property[RepeaterOffsetId] ?? 0.0);
                    var transformGroup = (PropertyValueGroup)(property[RepeaterTransformGroupId] ?? PropertyValueGroup.Empty);
                    var anchorPoint = (Vector3d)(transformGroup[RepeaterTransformAnchorPointId] ?? Vector3d.Zero);
                    var position = (Vector3d)(transformGroup[RepeaterTransformPositionId] ?? Vector3d.Zero);
                    var scale = (Vector3d)(transformGroup[RepeaterTransformScaleId] ?? Vector3d.Zero) * 0.01;
                    var angle = (double)(transformGroup[RepeaterTransformAngleId] ?? 0.0);
                    var beginPointOpacity = (double)(transformGroup[RepeaterTransformBeginPointOpacityId] ?? 0.0) * 0.01;
                    var endPointOpacity = (double)(transformGroup[RepeaterTransformEndPointOpacityId] ?? 0.0) * 0.01;

                    tree.ApplyRepeater(count, offset, anchorPoint, position, scale, angle, beginPointOpacity, endPointOpacity);
                }
                else if (property.TryGetValue(CombineTypeId, out var combineType))
                {
                    var operation = (ClippingOperation)(combineType ?? ClippingOperation.None);
                    var paths = tree.GetAllPaths();
                    ShapePath newShape;
                    if (operation != ClippingOperation.None)
                    {
                        var path = paths.First();
                        var option = new ShapeOptions
                        {
                            IntersectionRule = IntersectionRule.NonZero,
                            ClippingOperation = operation
                        };

                        newShape = new ShapePath(path.Clip(option, paths.Skip(1)));
                    }
                    else
                    {
                        newShape = new ShapePath(new PathCollection(paths));
                    }

                    tree = new ShapeGroupTree();
                    tree.AddNode(newShape);
                }
                else if (property.TryGetValue(TrimmingBeginId, out var trimmingBegin))
                {
                    var offset = (double)(property[TrimmingOffsetId] ?? 0.0);
                    offset = (((offset % 360.0) + 360.0) % 360.0) / 360.0;

                    var begin = (double)(trimmingBegin ?? 0.0) * 0.01;
                    var end = (double)(property[TrimmingEndId] ?? 0.0) * 0.01;
                    if (begin > end)
                    {
                        (end, begin) = (begin, end);
                    }

                    tree.ApplyTrimming((float)(begin + offset), (float)(end + offset));
                }
            }

            return tree;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Polygon[] ToPolygons(IPathCollection paths)
        {
            return [..paths.SelectMany(p => p.Flatten()).Select(p => new Polygon(p.Points.Span))];
        }
    }

    enum ShapeFillRule
    {
        NonZero,
        EvenOdd
    }

    enum GradientType
    {
        Linear,
        Radial
    }

    abstract class ShapeTreeBase
    {
        public abstract ShapeTreeBase Copy();

        protected internal abstract void Transform(Matrix3x2 matrix, float opacity);
    }

    class ShapeGroupTree : ShapeTreeBase
    {
        List<ShapeTreeBase> Nodes { get; } = [];

        public void AddNode(ShapeTreeBase node)
        {
            Nodes.Add(node);
        }

        public IEnumerable<(Brush brush, ShapeFillRule, BlendMode blendMode, IPathCollection paths)> GetDrawables(float downSampling)
        {
            foreach (var node in Nodes.AsEnumerable().Reverse())
            {
                if (node is ShapeGroupTree childGroup)
                {
                    foreach (var drawable in childGroup.GetDrawables(downSampling))
                    {
                        yield return drawable;
                    }
                    continue;
                }

                switch (node)
                {
                    case ShapeFillBase fill:
                        {
                            var brush = fill.GetBrush();
                            brush.Transform(Matrix3x3.CreateScale(downSampling, downSampling));
                            var path = TraversePath(fill);
                            path = path.Transform(Matrix3x2.CreateScale(downSampling));
                            yield return (brush, fill.FillRule, fill.BlendMode, path);
                        }
                        break;
                    case ShapeStrokeBase stroke:
                        {
                            var brush = stroke.GetBrush();
                            brush.Transform(Matrix3x3.CreateScale(downSampling, downSampling));
                            var path = (IPathCollection)new PathCollection(TraversePath(stroke).Select(p => p.GenerateOutline(stroke.Width)));
                            path = path.Transform(Matrix3x2.CreateScale(downSampling));
                            yield return (brush, ShapeFillRule.NonZero, stroke.BlendMode, path);
                        }
                        break;
                }
            }
        }

        public IEnumerable<IPath> GetAllPaths()
        {
            foreach (var node in Nodes.AsEnumerable().Reverse())
            {
                if (node is ShapeGroupTree childGroup)
                {
                    foreach (var path in  childGroup.GetAllPaths())
                    {
                        yield return path;
                    }
                }
                else if (node is ShapePath shape)
                {
                    foreach (var path in shape.Paths)
                    {
                        yield return path;
                    }
                }
            }
        }

        public void ApplyRepeater(int count, double offset, Vector3d anchorPoint, Vector3d position, Vector3d scale, double angle, double beginPointOpacity, double endPointOpacity)
        {
            if (count < 1)
            {
                Nodes.Clear();
                return;
            }

            var repeatTarget = Copy();
            Nodes.Clear();

            for (var i = 0; i < count; i++)
            {
                var move = offset + i;
                var transform = Matrix3x2.CreateTranslation((float)-anchorPoint.X, (float)-anchorPoint.Y) *
                    Matrix3x2.CreateScale((float)Math.Pow(scale.X, move), (float)Math.Pow(scale.Y, move)) *
                    Matrix3x2.CreateRotation((float)(angle * move / 180.0 * Math.PI)) *
                    Matrix3x2.CreateTranslation((float)(anchorPoint.X + position.X * move), (float)(anchorPoint.Y + position.Y * move));

                var newChild = repeatTarget.Copy();
                newChild.Transform(transform, (float)double.Lerp(beginPointOpacity, endPointOpacity, i / (double)count));
                AddNode(newChild);
            }
        }

        public void ApplyTrimming(float begin, float end)
        {
            foreach (var node in Nodes)
            {
                switch (node)
                {
                    case ShapeGroupTree childGroup:
                        childGroup.ApplyTrimming(begin, end);
                        break;
                    case ShapePath path:
                        path.ApplyTrimming(begin, end);
                        break;
                }
            }
        }

        public void Transform(Vector3d anchorPoint, Vector3d position, Vector3d scale, double skew, double skewAxis, double angle, double opacity)
        {
            var skewRad = skewAxis / 180.0F * Math.PI;
            var matrix = Matrix3x2.CreateTranslation((float)-anchorPoint.X, (float)-anchorPoint.Y) *
                Matrix3x2.CreateScale((float)scale.X, (float)scale.Y) *
                Matrix3x2.CreateSkew((float)(Math.Cos(skewRad) * skew), (float)(Math.Sin(skewRad) * skew)) *
                Matrix3x2.CreateRotation((float)(angle / 180.0 * Math.PI)) *
                Matrix3x2.CreateTranslation((float)position.X, (float)position.Y);
            Transform(matrix, (float)opacity);
        }

        public override ShapeTreeBase Copy()
        {
            var result = new ShapeGroupTree();
            result.Nodes.AddRange(Nodes.Select(n => n.Copy()));

            return result;
        }

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            foreach (var node in Nodes)
            {
                node.Transform(matrix, opacity);
            }
        }

        IPathCollection TraversePath(ShapeTreeBase? stop)
        {
            var resultPaths = new List<IPath>();

            foreach (var node in Nodes)
            {
                if (node == stop)
                {
                    break;
                }

                switch (node)
                {
                    case ShapeGroupTree childGroup:
                        resultPaths.AddRange(childGroup.TraversePath(null));
                        break;
                    case ShapePath path:
                        resultPaths.AddRange(path.Paths);
                        break;
                }
            }

            return new PathCollection(resultPaths);
        }
    }

    file class ShapePath : ShapeTreeBase
    {
        public IPathCollection Paths { get; private set; }

        public float Opacity { get; set; } = 1.0F;

        public ShapePath(IPathCollection paths)
        {
            Paths = paths;
        }

        public ShapePath(IPath path) : this(new PathCollection([path])) { }

        public void ApplyTrimming(float begin, float end)
        {
            if (begin == end)
            {
                Paths = new PathCollection();
                return;
            }
            else if (end - begin >= 1.0F)
            {
                return;
            }

            var newPaths = new List<IPath>();
            foreach (var path in Paths)
            {
                var flattenedPaths = path.Flatten().ToArray();
                var pathSegments = new (Vector2, Vector2, float)[flattenedPaths.Length == 1 ? 1 : flattenedPaths.Length * 2][];
                if (flattenedPaths.Length == 1 && flattenedPaths[0].IsClosed)
                {
                    var points = new PointF[flattenedPaths[0].Points.Length * 2];
                    flattenedPaths[0].Points.Span.CopyTo(points);
                    flattenedPaths[0].Points.Span.CopyTo(points.AsSpan(flattenedPaths[0].Points.Length));
                    var nextPoints = points.Skip(1).Append(points[0]);
                    pathSegments[0] = [..points.Zip(nextPoints, (f, s) =>
                    {
                        var fv = (Vector2)f;
                        var sv = (Vector2)s;
                        return (fv, sv, (sv - fv).Length());
                    })];
                }
                else
                {
                    for (var i = 0; i < flattenedPaths.Length; i++)
                    {
                        var points = flattenedPaths[i].Points.ToArray();
                        var nextPoints = points.Skip(1);
                        if (flattenedPaths[i].IsClosed)
                        {
                            nextPoints = nextPoints.Append(points[0]);
                        }
                        pathSegments[i] = [..points.Zip(nextPoints, (f, s) =>
                        {
                            var fv = (Vector2)f;
                            var sv = (Vector2)s;
                            return (fv, sv, (sv - fv).Length());
                        })];
                        pathSegments[i + flattenedPaths.Length] = pathSegments[i];
                    }
                }
                var pathLengths = pathSegments.Select(p => p.Sum(s => s.Item3)).ToArray();
                var totalLength = pathLengths.Sum();

                var trimmedPaths = TrimPath(flattenedPaths, pathSegments, pathLengths, totalLength, begin, end);

                if (path is ComplexPolygon)
                {
                    newPaths.Add(new ComplexPolygon(trimmedPaths));
                }
                else
                {
                    newPaths.AddRange(trimmedPaths);
                }
            }
            Paths = new PathCollection(newPaths);
        }

        static IEnumerable<Path> TrimPath(ISimplePath[] flattenedPaths, (Vector2, Vector2, float)[][] pathSegments, float[] pathLengths, float totalLength, float begin, float end)
        {
            var trimmedPaths = new List<Path>();

            var beginPosition = begin * totalLength * 0.5F;
            var endPosition = end * totalLength * 0.5F;
            var currentPosition = 0.0F;
            for (var i = 0; i < pathSegments.Length && currentPosition < endPosition; i++)
            {
                if (currentPosition + pathLengths[i] < beginPosition)
                {
                    currentPosition += pathLengths[i];
                    continue;
                }

                var bp = beginPosition - currentPosition;
                var ep = endPosition - currentPosition;
                if (bp <= 0.0F && ep >= pathLengths[i])
                {
                    trimmedPaths.Add(new Path(flattenedPaths[i % flattenedPaths.Length].Points.ToArray()));
                    currentPosition += pathLengths[i];
                    continue;
                }

                var newPoints = new List<PointF>();
                foreach (var (start, next, length) in pathSegments[i])
                {
                    if (bp - length > 0.0F)
                    {
                        bp -= length;
                        ep -= length;
                        continue;
                    }

                    if (bp < 0.0F)
                    {
                        newPoints.Add(start);
                    }
                    else
                    {
                        newPoints.Add(Vector2.Lerp(start, next, bp / length));
                    }

                    bp -= length;
                    ep -= length;

                    if (ep < 0.0F)
                    {
                        newPoints.Add(Vector2.Lerp(start, next, (ep + length) / length));
                    }
                    else if (ep == 0.0F)
                    {
                        newPoints.Add(next);
                    }

                    if (ep <= 0.0F)
                    {
                        break;
                    }
                }
                if (newPoints.Count > 1)
                {
                    trimmedPaths.Add(new Path([.. newPoints]));
                }

                currentPosition += pathLengths[i];
            }

            return trimmedPaths;
        }

        public override ShapeTreeBase Copy()
        {
            return new ShapePath(new PathCollection([..Paths]));
        }

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            Paths = Paths.Transform(matrix);
            Opacity *= opacity;
        }
    }

    file abstract class ShapeFillBase : ShapeTreeBase
    {
        public ShapeFillRule FillRule { get; }

        public BlendMode BlendMode { get; }

        public float Opacity { get; set; } = 1.0F;

        protected ShapeFillBase(ShapeFillRule fillRule, BlendMode blendMode)
        {
            FillRule = fillRule;
            BlendMode = blendMode;
        }

        public abstract Brush GetBrush();

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            Opacity *= opacity;
        }
    }

    file abstract class ShapeStrokeBase : ShapeTreeBase
    {
        public float Width { get; }

        public EndCapStyle EndCapStyle { get; }

        public JointStyle JoinStyle { get; }

        public BlendMode BlendMode { get; }

        public float Opacity { get; set; } = 1.0F;

        protected ShapeStrokeBase(float width, EndCapStyle endCapStyle, JointStyle joinStyle, BlendMode blendMode)
        {
            Width = width;
            EndCapStyle = endCapStyle;
            JoinStyle = joinStyle;
            BlendMode = blendMode;
        }

        public abstract Brush GetBrush();

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            Opacity *= opacity;
        }
    }

    file class ShapeSolidFill : ShapeFillBase
    {
        public Vector4 Color { get; }

        public ShapeSolidFill(Vector4 color, ShapeFillRule fillRule, BlendMode blendMode) : base(fillRule, blendMode)
        {
            Color = color;
        }

        public override ShapeTreeBase Copy()
        {
            return new ShapeSolidFill(Color, FillRule, BlendMode) { Opacity = Opacity };
        }

        public override Brush GetBrush()
        {
            var brushColor = Color;
            brushColor.W *= Opacity;
            return new SolidBrush(brushColor);
        }
    }

    file class ShapeGradientFill : ShapeFillBase
    {
        public ColorGradient Color { get; }

        public bool UseOkLabInterpolation { get; }

        public GradientType Type { get; }

        public Vector2 BeginPosition { get; set; }

        public Vector2 EndPosition { get; set; }

        public ShapeGradientFill(ColorGradient color, bool useOkLabInterpolation, GradientType type, Vector2 beginPosition, Vector2 endPosition, ShapeFillRule fillRule, BlendMode blendMode) : base(fillRule, blendMode)
        {
            Color = color;
            UseOkLabInterpolation = useOkLabInterpolation;
            Type = type;
            BeginPosition = beginPosition;
            EndPosition = endPosition;
        }

        public override ShapeTreeBase Copy()
        {
            return new ShapeGradientFill(Color, UseOkLabInterpolation, Type, BeginPosition, EndPosition, FillRule, BlendMode) { Opacity = Opacity };
        }

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            base.Transform(matrix, opacity);
            BeginPosition = Vector2.Transform(BeginPosition, matrix);
            EndPosition = Vector2.Transform(EndPosition, matrix);
        }

        public override Brush GetBrush()
        {
            if (Type == GradientType.Radial)
            {
                return new RadialGradientBrush(Color, UseOkLabInterpolation, BeginPosition, EndPosition);
            }
            else
            {
                return new LinearGradientBrush(Color, UseOkLabInterpolation, BeginPosition, EndPosition);
            }
        }
    }

    file class ShapeSolidStroke : ShapeStrokeBase
    {
        public Vector4 Color { get; }

        public ShapeSolidStroke(Vector4 color, float width, EndCapStyle endCapStyle, JointStyle joinStyle, BlendMode blendMode) : base(width, endCapStyle, joinStyle, blendMode)
        {
            Color = color;
        }

        public override ShapeTreeBase Copy()
        {
            return new ShapeSolidStroke(Color, Width, EndCapStyle, JoinStyle, BlendMode) { Opacity = Opacity };
        }

        public override Brush GetBrush()
        {
            var brushColor = Color;
            brushColor.W *= Opacity;
            return new SolidBrush(brushColor);
        }
    }

    file class ShapeGradientStroke : ShapeStrokeBase
    {
        public ColorGradient Color { get; }

        public bool UseOkLabInterpolation { get; }

        public GradientType Type { get; }

        public Vector2 BeginPosition { get; set; }

        public Vector2 EndPosition { get; set; }

        public ShapeGradientStroke(ColorGradient color, bool useOkLabInterpolation, GradientType type, Vector2 beginPosition, Vector2 endPosition, float width, EndCapStyle endCapStyle, JointStyle joinStyle, BlendMode blendMode) : base(width, endCapStyle, joinStyle, blendMode)
        {
            Color = color;
            UseOkLabInterpolation = useOkLabInterpolation;
            Type = type;
            BeginPosition = beginPosition;
            EndPosition = endPosition;
        }

        public override ShapeTreeBase Copy()
        {
            return new ShapeGradientStroke(Color, UseOkLabInterpolation, Type, BeginPosition, EndPosition, Width, EndCapStyle, JoinStyle, BlendMode) { Opacity = Opacity };
        }

        protected internal override void Transform(Matrix3x2 matrix, float opacity)
        {
            base.Transform(matrix, opacity);
            BeginPosition = Vector2.Transform(BeginPosition, matrix);
            EndPosition = Vector2.Transform(EndPosition, matrix);
        }

        public override Brush GetBrush()
        {
            if (Type == GradientType.Radial)
            {
                return new RadialGradientBrush(Color, UseOkLabInterpolation, BeginPosition, EndPosition);
            }
            else
            {
                return new LinearGradientBrush(Color, UseOkLabInterpolation, BeginPosition, EndPosition);
            }
        }
    }
}
