using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;

namespace NiVE3.Cache
{
    class ImageCache
    {
        private static readonly ImageCache Instance = new ImageCache();

        private Dictionary<Int128, NManagedImage> CachedImages { get; } = new Dictionary<Int128, NManagedImage>();

        private int CachedSize { get; set; }

        private ImageCache() { }

        private NManagedImage? GetInternal(in Int128 key)
        {
            if (CachedImages.TryGetValue(key, out var image))
            {
                return image;
            }
            else
            {
                return null;
            }
        }

        private bool TryGetInternal(in Int128 key, [NotNullWhen(true)] out NManagedImage? image)
        {
            return CachedImages.TryGetValue(key, out image);
        }

        private void AddInternal(NManagedImage image, in Int128 key)
        {
            if (CachedImages.ContainsKey(key))
            {
                var oldImage = CachedImages[key];
                CachedSize -= oldImage.DataLength;
                oldImage.Dispose();
                CachedImages[key] = image;
            }
            else
            {
                CachedImages.Add(key, image);
            }
            CachedSize = image.DataLength;
        }

        public static NManagedImage? Get(in Int128 key)
        {
            return Instance.GetInternal(key);
        }

        public static bool TryGet(in Int128 key, [NotNullWhen(true)] out NManagedImage? image)
        {
            return Instance.TryGetInternal(key, out image);
        }

        public static void Add(NManagedImage image, in Int128 key)
        {
            Instance.AddInternal(image, key);
        }
    }
}
