using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;

namespace NiVE3.Cache
{
    class ImageCache
    {
        private static readonly ImageCache Instance = new ImageCache();

        public static bool EnableCompress { get; set; } = true;

        private DualKeyDictionary<(Guid, double), Int128, (Guid, Int128), (NManagedImage, ROI)> CachedImages { get; } = [];

        private int CachedSize { get; set; }

        private ImageCache() { }

        private (NManagedImage, ROI)? GetInternal(in Guid objectId, in Int128 key, double time)
        {
            if (CachedImages.TryGetValue((objectId, time), key, out var image))
            {
                return image;
            }
            else
            {
                return null;
            }
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, double time, out (NManagedImage, ROI) image)
        {
            return CachedImages.TryGetValue((objectId, time), key, out image);
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, out (NManagedImage, ROI) image)
        {
            if (CachedImages.TryGetValues((objectId, key), out var values))
            {
                image = values[0];
                return true;
            }
            else
            {
                image = (null!, ROI.Empty);
                return false;
            }
        }

        private void AddInternal(in Guid objectId, in Int128 key, double time, (NManagedImage, ROI) image)
        {
            var updateKey = (objectId, time);
            if (CachedImages.ContainsUpdateKey(updateKey))
            {
                var oldImages = CachedImages.GetUpdateTargetKeys(updateKey).Select(k => CachedImages[updateKey, k]).ToArray();
                CachedImages.Update(updateKey, key, (objectId, key), image);

                foreach (var i in oldImages)
                {
                    CachedSize -= i.Item1.DataLength;
                    i.Item1.Dispose();
                }
            }
            else
            {
                CachedImages.Add(updateKey, key, (objectId, key), image);
            }
            CachedSize += image.Item1.DataLength;
        }

        private void ClearInternal()
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

        public static void Add(in Guid objectId, in Int128 key, double time, (NManagedImage, ROI) image)
        {
            Instance.AddInternal(objectId, key, time, image);
        }

        public static void Clear()
        {
            Instance.ClearInternal();
        }
    }
}
