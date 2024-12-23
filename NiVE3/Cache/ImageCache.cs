using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Config;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;

namespace NiVE3.Cache
{
    class ImageCache
    {
        static readonly int ImageElementSize = Marshal.SizeOf<Vector4>();

        static ImageCache Instance { get; } = new ImageCache();

        static bool IsCompressCache { get; set; }

        long CacheLimit { get; set; } = Math.Min(16L * 1024 * Const.MiB, SystemInfo.MaxCacheLimit);

        private DualKeyDictionary<Guid, (Time, Int128), (Guid, Int128), (IDisposable, long, ROI)> CachedImages { get; } = [];

        private long CachedSize { get; set; }

        private CacheKeyLru KeyLru { get; } = new CacheKeyLru();

        private ImageCache()
        {
            CacheLimit = Math.Min(ApplicationSetting.Setting.ImageCacheLimit * Const.MiB, SystemInfo.MaxCacheLimit);
            ApplicationSetting.Setting.UpdateSetting += Setting_UpdateSetting;
        }

        private (NManagedImage, ROI)? GetInternal(in Guid objectId, in Int128 key, Time time)
        {
            if (CachedImages.TryGetValue(objectId, (time, key), out var image))
            {
                KeyLru.Add(objectId, key, time);
                return Decompress(image);
            }
            else
            {
                return null;
            }
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, Time time, out (NManagedImage, ROI) image)
        {
            var result = CachedImages.TryGetValue(objectId, (time, key), out var compressedImage);
            if (result)
            {
                image = Decompress(compressedImage);
                KeyLru.Add(objectId, key, time);
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
                KeyLru.UpdateLastAccessBySecondaryKey(objectId, key);
                return true;
            }
            else
            {
                image = (null!, ROI.Empty);
                return false;
            }
        }

        private void AddInternal(Guid objectId, in Int128 key, Time time, NManagedImage image, ROI roi)
        {
            (IDisposable, int, ROI) compressedImage;
            // NOTE: 実際に確保したメモリの容量で判定する
            var managedImageSize = image.Data.Length * ImageElementSize;
            if (IsCompressCache)
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
                        KeyLru.Remove(objectId, k.Item2, k.Item1);
                    }
                }

                CachedImages.Add(objectId, (time, key), (objectId, key), compressedImage);
            }
            else
            {
                CachedImages.Add(objectId, (time, key), (objectId, key), compressedImage);
            }
            KeyLru.Add(objectId, key, time);
            CachedSize += compressedImage.Item2;

            while (CachedSize > CacheLimit)
            {
                var primaryKey = KeyLru.RemoveLast();
                if (primaryKey.Item1 == Guid.Empty)
                {
                    break;
                }

                var oldImage = CachedImages[primaryKey.Item1, (primaryKey.Item3, primaryKey.Item2)];
                oldImage.Item1.Dispose();
                CachedSize -= oldImage.Item2;
                CachedImages.Remove(primaryKey.Item1, (primaryKey.Item3, primaryKey.Item2));
            }
        }

        private Time[] GetCachedTimeInternal(in Guid objectId)
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

        private void ClearInternal(in Guid objectId)
        {
            if (!CachedImages.ContainsUpdateKey(objectId))
            {
                return;
            }

            foreach (var k in CachedImages.GetUpdateTargetKeys(objectId))
            {
                var image = CachedImages[objectId, k];
                image.Item1.Dispose();
                CachedSize -= image.Item2;
                KeyLru.Remove(objectId, k.Item2, k.Item1);
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
            KeyLru.Clear();
            CachedSize = 0;
        }

        private void Setting_UpdateSetting(object? sender, EventArgs e)
        {
            CacheLimit = ApplicationSetting.Setting.ImageCacheLimit * Const.MiB;
            if (ApplicationSetting.Setting.IsCompressCache != IsCompressCache)
            {
                ClearAllInternal();
                IsCompressCache = ApplicationSetting.Setting.IsCompressCache;
            }
        }

        public static (NManagedImage, ROI)? Get(in Guid objectId, in Int128 key, Time time)
        {
            return Instance.GetInternal(objectId, key, time);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, Time time, out (NManagedImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, time, out image);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, out (NManagedImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, out image);
        }

        public static void Add(in Guid objectId, in Int128 key, Time time, NManagedImage image, ROI roi)
        {
            Instance.AddInternal(objectId, key, time, image, roi);
        }

        public static Time[] GetCachedTime(in Guid objectId)
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

        static (NManagedImage, ROI) Decompress((IDisposable, long, ROI) compressedImage)
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
