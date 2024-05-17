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

        private DualKeyDictionary<Tuple<Guid, Int128, double>, Tuple<Guid, Int128>, Tuple<NManagedImage, ROI>> CachedImages { get; } = [];

        private int CachedSize { get; set; }

        private ImageCache() { }

        private Tuple<NManagedImage, ROI>? GetInternal(in Guid objectId, in Int128 key, double time)
        {
            if (CachedImages.TryGetValue(Tuple.Create(objectId, key, time), out var image))
            {
                return image;
            }
            else
            {
                return null;
            }
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, double time, [NotNullWhen(true)] out Tuple<NManagedImage, ROI>? image)
        {
            return CachedImages.TryGetValue(Tuple.Create(objectId, key, time), out image);
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, [NotNullWhen(true)] out Tuple<NManagedImage, ROI>? image)
        {
            if (CachedImages.TryGetValues(Tuple.Create(objectId, key), out var values))
            {
                image = values[0];
                return true;
            }
            else
            {
                image = null;
                return false;
            }
        }

        private void AddInternal(in Guid objectId, in Int128 key, double time, Tuple<NManagedImage, ROI> image)
        {
            var primaryKey = Tuple.Create(objectId, key, time);
            if (CachedImages.ContainsKey(primaryKey))
            {
                var oldImage = CachedImages[primaryKey];
                CachedSize -= oldImage.Item1.DataLength;
                oldImage.Item1.Dispose();
            }
            CachedImages.Add(primaryKey, Tuple.Create(objectId, key), image);
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

        public static Tuple<NManagedImage, ROI>? Get(in Guid objectId, in Int128 key, double time)
        {
            return Instance.GetInternal(objectId, key, time);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, double time, [NotNullWhen(true)] out Tuple<NManagedImage, ROI>? image)
        {
            return Instance.TryGetInternal(objectId, key, time, out image);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, [NotNullWhen(true)] out Tuple<NManagedImage, ROI>? image)
        {
            return Instance.TryGetInternal(objectId, key, out image);
        }

        public static void Add(in Guid objectId, in Int128 key, double time, Tuple<NManagedImage, ROI> image)
        {
            Instance.AddInternal(objectId, key, time, image);
        }

        public static void Clear()
        {
            Instance.ClearInternal();
        }
    }
}
