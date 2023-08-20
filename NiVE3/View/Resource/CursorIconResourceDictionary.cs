using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;
using System.Windows.Controls;
using NiVE3.Extension;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary]
    class CursorIconResourceDictionary : ResourceDictionary
    {
        const double CursorImageMargin = 1.5;

        [ShowInMarkup, CursorInfo("SizeWOnly")]
        public static readonly string SizeWOnly = nameof(SizeWOnly);

        [ShowInMarkup, CursorInfo("SizeEOnly")]
        public static readonly string SizeEOnly = nameof(SizeEOnly);

        public CursorIconResourceDictionary()
        {
            var keys = typeof(CursorIconResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => ((string)f.GetValue(null)!, f.GetCustomAttribute<CursorInfoAttribute>()));
            foreach (var (key, attr) in keys)
            {
                if (attr == null)
                {
                    return;
                }

                this[key] = CreateCursor(attr.GeometryResourceName, new Point(attr.HotSpotX, attr.HotSpotY));
            }
        }

        static Cursor CreateCursor(string iconName, Point hotSpot)
        {
            using (var pngStream = CreatePng(iconName))
            {
                var cursorStream = new MemoryStream();
                using (var writer = new BinaryWriter(cursorStream, Encoding.Default, true))
                {
                    writer.WriteStruct(new ICONDIR(2, 1));
                    writer.WriteStruct(new ICONDIRENTRY(CursorInfoAttribute.CursorSize, CursorInfoAttribute.CursorSize, hotSpot, pngStream.Length));
                }
                pngStream.CopyTo(cursorStream);

                cursorStream.Seek(0, SeekOrigin.Begin);
                return new Cursor(cursorStream);
            }
        }

        static Stream CreatePng(string iconName)
        {
            const string PathName = "IconPath";

            var geometry = Application.Current.Resources.MergedDictionaries.FirstOrDefault(rd => rd.Contains(iconName))?[iconName] as Geometry;
            var render = new RenderTargetBitmap(CursorInfoAttribute.CursorSize, CursorInfoAttribute.CursorSize, 96, 96, PixelFormats.Pbgra32);
            var grid = new Grid() { Background = Brushes.Transparent };
            grid.Children.Add(new System.Windows.Shapes.Path() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Data = geometry, Fill = Brushes.White, Stroke = Brushes.White, StrokeThickness = 1.5, Name = PathName });
            grid.Children.Add(new System.Windows.Shapes.Path() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Data = geometry, Fill = Brushes.Black });
            grid.Measure(new Size(CursorInfoAttribute.CursorSize, CursorInfoAttribute.CursorSize));

            var scale = (CursorInfoAttribute.CursorSize - CursorImageMargin) / Math.Max(grid.DesiredSize.Width, grid.DesiredSize.Height);
            grid.LayoutTransform = new ScaleTransform(scale, scale);
            var parentGrid = new Grid();
            parentGrid.Children.Add(grid);

            parentGrid.Measure(new Size(CursorInfoAttribute.CursorSize, CursorInfoAttribute.CursorSize));
            parentGrid.Arrange(new Rect(0.0, 0.0, CursorInfoAttribute.CursorSize, CursorInfoAttribute.CursorSize));
            render.Render(parentGrid);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));
            var result = new MemoryStream();
            encoder.Save(result);
            result.Seek(0, SeekOrigin.Begin);

            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    file sealed class CursorInfoAttribute : Attribute
    {
        public const int CursorSize = 20;

        public string GeometryResourceName { get; }

        public double HotSpotX { get; set; } = CursorSize * 0.5;

        public double HotSpotY { get; set; } = CursorSize * 0.5;

        public CursorInfoAttribute(string geometryResourceName)
        {
            GeometryResourceName = geometryResourceName;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    file struct ICONDIR
    {
        public short icoReserved;
        public short icoResourceType;
        public short icoResourceCount;

        public ICONDIR(short type, short count)
        {
            icoReserved = 0;
            icoResourceType = type;
            icoResourceCount = count;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    file struct ICONDIRENTRY
    {
        public byte bWidth;
        public byte bHeight;
        public byte bColorCount;
        public byte bReserved;
        public short wHotSpotX;
        public short wHotSpotY;
        public uint dwBytesInRes;
        public uint dwImageOffset;

        public ICONDIRENTRY(int width, int height, Point hotSpot, long imageSize) : this((byte)width, (byte)height, (short)hotSpot.X, (short)hotSpot.Y, (uint)imageSize) { }

        public ICONDIRENTRY(byte width, byte height, short hotSpotX, short hotSpotY, uint imageSize)
        {
            bWidth = width;
            bHeight = height;
            bColorCount = 0;
            bReserved = 0;
            wHotSpotX = hotSpotX;
            wHotSpotY = hotSpotY;
            dwBytesInRes = imageSize;
            dwImageOffset = 22;
        }
    }
}
