using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
using NiVE3.View.Resource;
using SixLabors.ImageSharp.Drawing;

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

        const string SolidStrokeGroupId = nameof(SolidStrokeGroupId);

        const string SolidStrokeColorId = nameof(SolidStrokeColorId);

        const string SolidStrokeOpacity = nameof(SolidStrokeOpacity);

        const string SolidStrokeWidthId = nameof(SolidStrokeWidthId);

        const string SolidStrokeEndCapTypeId = nameof(SolidStrokeEndCapTypeId);

        const string SolidStrokeJoinStyleTypeId = nameof(SolidStrokeJoinStyleTypeId);

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
                new AppendablePropertyItem(RectangleGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RectangleGroup), () =>
                    new PropertyGroup(RectangleGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RectangleGroup),
                    [
                        new Vector3dProperty(RectangleSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Size), new Vector3d(100.0), Vector3d.Zero, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Pixel), separator: ",", useLinkRatio: true),
                        new Vector3dProperty(RectanglePositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Position), new Vector3d(), digit: 2)
                    ])),
                new AppendablePropertyItem(CircleGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_CircleGroup), () =>
                    new PropertyGroup(CircleGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_CircleGroup),
                    [
                        new Vector3dProperty(CircleSizeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Size), new Vector3d(100.0), Vector3d.Zero, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Pixel), separator: ",", useLinkRatio: true),
                        new Vector3dProperty(CirclePositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Position), new Vector3d(), digit: 2)
                    ])),
                new AppendablePropertyItem(RegularPolygonGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RegularPolygonGroup), () =>
                    new PropertyGroup(RegularPolygonGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RegularPolygonGroup),
                    [
                        new DoubleProperty(RegularPolygonPointCountId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_PolygonGroup_Points), 5.0, 3.0, 10000.0, digit: 0),
                        new DoubleProperty(RegularPolygonRadiusId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RegularPolygonGroup_Radius), 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(RegularPolygonRoundedId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RegularPolygonGroup_Rounded), 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                        new Vector3dProperty(RegularPolygonPositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Position), Vector3d.Zero, digit: 2),
                        new AngleProperty(RegularPolygonAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Angle), 0.0, digit: 2)
                    ])),
                new AppendablePropertyItem(StarGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup), () =>
                    new PropertyGroup(StarGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup),
                    [
                        new DoubleProperty(StarPointCountId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_PolygonGroup_Points), 5.0, 3.0, 10000.0, digit: 0),
                        new DoubleProperty(StarOuterRadiusId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup_OuterRadius), 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(StarInnerRadiusId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup_InnerRadius), 50.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(StarOuterRoundedId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup_OuterRounded), 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                        new DoubleProperty(StarInnerRoundedId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_StarGroup_InnerRounded), 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                        new Vector3dProperty(StarPositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Position), Vector3d.Zero, digit: 2),
                        new AngleProperty(StarAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_ShapeObjectGroup_Angle), 0.0, digit: 2)
                    ])),
                AppendablePropertyItemSeparator.Instance,
                new AppendablePropertyItem(SolidFillGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidFillGroup), () =>
                    new PropertyGroup(SolidFillGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidFillGroup),
                    [
                        new EnumProperty(SolidFillRuleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidFillGroup_FillRule), typeof(ShapeFillRule), typeof(LanguageResourceDictionary), ShapeFillRule.NonZero, selectBoxWidth: 100.0),
                        new ColorProperty(
                            SolidFillColorId,
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Color),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Color),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_OK),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_Cancel),
                            Vector4.One
                        ),
                        new DoubleProperty(SolidFillOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Opacity), 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent))
                    ])),
                new AppendablePropertyItem(SolidStrokeGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidStrokeGroup), () =>
                    new PropertyGroup(SolidStrokeGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidStrokeGroup),
                    [
                        new ColorProperty(
                            SolidStrokeColorId,
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Color),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Color),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_OK),
                            LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Dialog_Cancel),
                            new Vector4(0.0F, 0.0F, 1.0F, 1.0F)
                        ),
                        new DoubleProperty(SolidStrokeOpacity, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidBrushGroup_Opacity), 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                        new DoubleProperty(SolidStrokeWidthId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidStrokeGroup_Width), 4.0, 0.0, double.MaxValue, digit: 2),
                        new EnumProperty(SolidStrokeEndCapTypeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidStrokeGroup_EndCapType), typeof(EndCapStyle), typeof(LanguageResourceDictionary), EndCapStyle.Square, selectBoxWidth: 100.0),
                        new EnumProperty(SolidStrokeJoinStyleTypeId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_SolidStrokeGroup_JoinStyleType), typeof(JointStyle), typeof(LanguageResourceDictionary), JointStyle.Square, selectBoxWidth: 100.0)
                    ])),
                AppendablePropertyItemSeparator.Instance,
                new AppendablePropertyItem(RepeaterGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup), () =>
                    new PropertyGroup(RepeaterGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup),
                    [
                        new DoubleProperty(RepeaterCountId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup_Count), 3.0, 0.0, double.MaxValue, digit: 1),
                        new DoubleProperty(RepeaterOffsetId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup_Offset), 0.0, double.MinValue, double.MaxValue, digit: 1),
                        new PropertyGroup(RepeaterTransformGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform),
                        [
                            new Vector3dProperty(RepeaterTransformAnchorPointId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_AnchorPoint), Vector3d.Zero, digit: 2),
                            new Vector3dProperty(RepeaterTransformPositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Position), new Vector3d(100.0, 0.0, 0.0), digit: 2),
                            new Scale3dProperty(RepeaterTransformScaleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Scale), new Vector3d(100.0), digit: 2),
                            new AngleProperty(RepeaterTransformAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Angle), 0.0, digit: 2),
                            new DoubleProperty(RepeaterTransformBeginPointOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity), 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent)),
                            new DoubleProperty(RepeaterTransformEndPointOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_RepeaterGroup_Transform_EndPointOpacity), 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.Unit_Percent))
                        ])
                    ]))
            ];
            groupItems[0] = new AppendablePropertyItem(GroupPropertyId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Group), () =>
                new PropertyGroup(GroupPropertyId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Group),
                [
                    new AppendableProperty(GroupContentPropertyId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Group_Content), groupItems),
                    new PropertyGroup(GroupTransformGroupId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform),
                    [
                        new Vector3dProperty(GroupTransformAnchorPointId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_AnchorPoint), Vector3d.Zero, digit: 2),
                        new Vector3dProperty(GroupTransformPositionId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Position), Vector3d.Zero, digit: 2),
                        new Scale3dProperty(GroupTransformScaleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Scale), new Vector3d(100.0), digit: 2),
                        new DoubleProperty(GroupTransformSkewId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Skew), 0.0, -100.0, 100.0, digit: 2),
                        new AngleProperty(GroupTransformSkewAxisId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_SkewAxis), 0.0, digit: 2),
                        new AngleProperty(GroupTransformAngleId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Angle), 0.0, digit: 2),
                        new DoubleProperty(GroupTransformOpacityId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Transform_Opacity), 100.0, 0.0, 100.0, digit: 2)
                    ])
                ]));
            return
            [
                new AppendableProperty(ContentPropertyId, LanguageResourceDictionary.CreateLanguageResourceKey(LanguageResourceDictionary.ShapeProperty_Content), groupItems)
            ];
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(double time, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }

        public NImage ReadFrame(double time, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }
    }

    enum ShapeFillRule
    {
        NonZero,
        EvenOdd
    }
}
