using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImTools;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;

namespace NiVE3.Cache
{
    class ImageCache
    {
        static readonly ImageCache Instance = new ImageCache();

        static readonly int ImageElementSize = Marshal.SizeOf<Vector4>();

        public static bool EnableCompress { get; set; }

        private DualKeyDictionary<Guid, (double, Int128), (Guid, Int128), (IDisposable, int, ROI)> CachedImages { get; } = [];

        private int CachedSize { get; set; }

        private ImageCache() { }

        private (NManagedImage, ROI)? GetInternal(in Guid objectId, in Int128 key, double time)
        {
            if (CachedImages.TryGetValue(objectId, (time, key), out var image))
            {
                return Decompress(image);
            }
            else
            {
                return null;
            }
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, double time, out (NManagedImage, ROI) image)
        {
            var result = CachedImages.TryGetValue(objectId, (time, key), out var compressedImage);
            if (result)
            {
                image = Decompress(compressedImage);
            }
            else
            {
                image = (null!, ROI.Empty);
            }
            return result;
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, out (NManagedImage, ROI) image)
        {
            if (CachedImages.TryGetValues((objectId, key), out var values))
            {
                image = Decompress(values[0]);
                return true;
            }
            else
            {
                image = (null!, ROI.Empty);
                return false;
            }
        }

        private void AddInternal(Guid objectId, in Int128 key, double time, NManagedImage image, ROI roi)
        {
            (IDisposable, int, ROI) compressedImage;
            // NOTE: 実際に確保したメモリの容量で判定する
            var managedImageSize = image.Data.Length * ImageElementSize;
            if (EnableCompress)
            {
                var qoiImage = Qoi.Encode(image);
                var size = qoiImage.GetAllocatedSize();
                if (size < managedImageSize)
                {
                    compressedImage = (qoiImage, size, roi);
                }
                else
                {
                    qoiImage.Dispose();
                    compressedImage = (image.Copy(), managedImageSize, roi);
                }
            }
            else
            {
                compressedImage = (image.Copy(), managedImageSize, roi);
            }
            if (CachedImages.ContainsUpdateKey(objectId))
            {
                var oldImageKeys = CachedImages.GetUpdateTargetKeys(objectId).Where(k => k.Item1 == time).ToArray();
                if (oldImageKeys.Length > 0)
                {
                    foreach (var k in oldImageKeys)
                    {
                        var oldImage = CachedImages[objectId, k];
                        CachedImages.Remove(objectId, k);
                        CachedSize -= oldImage.Item2;
                        oldImage.Item1.Dispose();
                    }
                }

                CachedImages.Add(objectId, (time, key), (objectId, key), compressedImage);
            }
            else
            {
                CachedImages.Add(objectId, (time, key), (objectId, key), compressedImage);
            }
            CachedSize += compressedImage.Item2;
        }

        private double[] GetCachedTimeInternal(Guid objectId)
        {
            if (CachedImages.ContainsUpdateKey(objectId))
            {
                return CachedImages.GetUpdateTargetKeys(objectId).Select(t => t.Item1).ToArray();
            }
            else
            {
                return [];
            }
        }

        private void ClearInternal(Guid objectId)
        {
            if (!CachedImages.ContainsUpdateKey(objectId))
            {
                return;
            }

            foreach (var (image, _, _) in CachedImages.GetUpdateTargetKeys(objectId).Select(k => CachedImages[objectId, k]))
            {
                image.Dispose();
            }
            CachedImages.Remove(objectId);
        }

        private void ClearAllInternal()
        {
            foreach (var image in CachedImages.Values)
            {
                image.Item1.Dispose();
            }
            CachedImages.Clear();
            CachedSize = 0;
        }

        public static (NManagedImage, ROI)? Get(in Guid objectId, in Int128 key, double time)
        {
            return Instance.GetInternal(objectId, key, time);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, double time, out (NManagedImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, time, out image);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, out (NManagedImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, out image);
        }

        public static void Add(in Guid objectId, in Int128 key, double time, NManagedImage image, ROI roi)
        {
            Instance.AddInternal(objectId, key, time, image, roi);
        }

        public static double[] GetCachedTime(in Guid objectId)
        {
            return Instance.GetCachedTimeInternal(objectId);
        }

        public static void Clear(in Guid objectId)
        {
            Instance.ClearInternal(objectId);
        }

        public static void ClearAll()
        {
            Instance.ClearAllInternal();
        }

        static (NManagedImage, ROI) Decompress((IDisposable, int, ROI) compressedImage)
        {
            var (image, _, roi) = compressedImage;
            if (image is NManagedImage notCompressed)
            {
                return ((NManagedImage)notCompressed.Copy(), roi);
            }
            else
            {
                return (Qoi.Decode((SlicedQoiImage)image), roi);
            }
        }
    }
}
