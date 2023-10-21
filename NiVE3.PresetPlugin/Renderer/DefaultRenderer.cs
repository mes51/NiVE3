using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Struct;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using NiVE3.PresetPlugin.Internal.Drawing;
using System.Runtime.Intrinsics;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "D67AC3F-A137-45B1-99F7-3E68A0B910E6", LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class DefaultRenderer : IRenderer
    {
        int Width { get; set; }

        int Height { get; set; }

        NManagedImage? CurrentFrame { get; set; }

        bool UseGpu { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void BeginRendering(double downSamplingRate, bool useGpu)
        {
            if (CurrentFrame != null)
            {
                throw new InvalidOperationException("rendering is already started"); // bug
            }

            CurrentFrame = new NManagedImage(Width, Height, true);
            UseGpu = useGpu;
        }

        public void Render(RenderableImage[] images)
        {
            if (CurrentFrame == null)
            {
                return;
            }

            foreach (var group in images.GroupByPrev(i => i.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var renderer = new Renderer3D(CurrentFrame);

                    // TODO: カメラ
                    const double Fov = 39.5978 / 180.0 * Math.PI;
                    var zoom = CurrentFrame.Width / Math.Tan(Fov * 0.5) * 0.5;
                    renderer.ViewMatrix = Matrix4x4d.CreateLookAt(Vector256.Create(0.5, 0.5, (zoom / CurrentFrame.Width), 0.0), Vector256.Create(0.5, 0.5, 0.0, 0.0), Vector256.Create(0.0, 1.0, 0.0, 0.0));
                    renderer.ProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(Fov, 1.0, double.Epsilon, double.PositiveInfinity);

                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var model = GetTransform3D(i.Transform, renderer.Size);

                        foreach (var (type, parentTransform) in i.ParentTransforms)
                        {
                            model = model * GetTransform3D(parentTransform, renderer.Size);
                        }

                        renderer.ModelMatrix = model;
                        renderer.AddRect(i.Image, i.BlendMode);
                    }

                    renderer.Render();
                }
                else
                {
                    var renderer = new Renderer2D(CurrentFrame);

                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var matrix = GetTransform2D(i.Transform);

                        foreach (var (type, parentTransform) in i.ParentTransforms)
                        {
                            matrix = GetTransform2D(parentTransform) * matrix;
                        }

                        renderer.Draw(i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode);
                    }
                }
            }
        }

        public NImage GetCurrentRenderedImage()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }
            
            return CurrentFrame.Copy();
        }

        public NImage FinishRendering()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }

            var result = CurrentFrame;
            CurrentFrame = null;
            return result;
        }

        static Matrix3x3 GetTransform2D(PropertyValueGroup transformProperties)
        {
            var anchorPoint = (Vector3d)(transformProperties[ILayerObject.TransformAnchorPointId] ?? new Vector3d());
            var scale = (Vector3d)(transformProperties[ILayerObject.TransformScaleId] ?? new Vector3d()) * 0.01;
            var angle = (double)(transformProperties[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transformProperties[ILayerObject.TransformTranslateId] ?? new Vector3d());
            return Matrix3x3.AffineTransform((Vector2)anchorPoint.AsVector2d(), (Vector2)scale.AsVector2d(), (float)angle, (Vector2)translate.AsVector2d());
        }

        static Matrix4x4d GetTransform3D(PropertyValueGroup transformProperties, double rendererSize)
        {
            var anchorPoint = (Vector3d)(transformProperties[ILayerObject.TransformAnchorPointId] ?? new Vector3d()) / rendererSize;
            var scale = (Vector3d)(transformProperties[ILayerObject.TransformScaleId] ?? new Vector3d()) * 0.01;
            var direction = (Vector3d)(transformProperties[ILayerObject.TransformDirectionId] ?? new Vector3d());
            var angleX = (double)(transformProperties[ILayerObject.TransformXAngleId] ?? 0.0);
            var angleY = (double)(transformProperties[ILayerObject.TransformYAngleId] ?? 0.0);
            var angleZ = (double)(transformProperties[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transformProperties[ILayerObject.TransformTranslateId] ?? new Vector3d()) / rendererSize;

            return Matrix4x4d.AffineTransform(anchorPoint, scale, direction, angleX, angleY, angleZ, translate);
        }

        public void Dispose()
        {
            CurrentFrame?.Dispose();
        }
    }
}
