using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host.GL
{
    /// <summary>
    /// OpenGL Render Suite でプラグインへ渡すテクスチャ
    /// OFX 上ではプロパティセットのハンドルがテクスチャのハンドルになります
    /// </summary>
    public sealed unsafe class OfxGlTexture : IDisposable
    {
        static readonly ConcurrentDictionary<nint, OfxGlTexture> Registry = new ConcurrentDictionary<nint, OfxGlTexture>();

        static long NextId;

        public uint TextureId { get; }

        public int Width { get; }

        public int Height { get; }

        public PropertySet Properties { get; }

        public nint Handle => Properties.Handle;

        public bool Disposed { get; private set; }

        /// <summary>
        /// ホストが所有するテクスチャ (Output) かどうか。true の場合 clipFreeTexture では解放されません
        /// </summary>
        public bool HostOwned { get; init; }

        OfxGlTexture(uint textureId, int width, int height, string name)
        {
            TextureId = textureId;
            Width = width;
            Height = height;

            Properties = new PropertySet($"GlTexture:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImage);
            Properties.SetAll(OfxNames.ImageEffectPropOpenGLTextureIndex, (int)textureId);
            Properties.SetAll(OfxNames.ImageEffectPropOpenGLTextureTarget, (int)GlNative.GL_TEXTURE_2D);
            Properties.SetAll(OfxNames.ImagePropBounds, 0, 0, width, height);
            Properties.SetAll(OfxNames.ImagePropRegionOfDefinition, 0, 0, width, height);
            Properties.SetAll(OfxNames.ImagePropRowBytes, 0);
            Properties.SetAll(OfxNames.ImagePropField, OfxNames.ImageFieldNone);
            Properties.SetAll(OfxNames.ImageEffectPropComponents, OfxNames.ComponentRGBA);
            Properties.SetAll(OfxNames.ImageEffectPropPixelDepth, OfxNames.BitDepthFloat);
            Properties.SetAll(OfxNames.ImageEffectPropPreMultiplication, OfxNames.ImageUnPreMultiplied);
            Properties.SetAll(OfxNames.ImageEffectPropRenderScale, 1.0, 1.0);
            Properties.SetAll(OfxNames.ImagePropPixelAspectRatio, 1.0);
            Properties.SetAll(OfxNames.ImagePropUniqueIdentifier, $"{name}#{Interlocked.Increment(ref NextId)}");

            Registry[Handle] = this;
        }

        /// <summary>
        /// BGRA (上から下) の画像データから GL_RGBA32F テクスチャを作成します。GL スレッド上で呼び出してください
        /// </summary>
        /// <param name="pixels">画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成されたテクスチャ</returns>
        public static OfxGlTexture CreateFromBgraTopDown(ReadOnlySpan<Vector4> pixels, int width, int height, string name)
        {
            // OFX の向き (下から上・RGBA) に変換してからアップロードする
            var buffer = (float*)NativeMemory.Alloc((nuint)((long)width * height * 4 * sizeof(float)));
            try
            {
                ImageBridge.ToOfx(pixels, width, height, buffer);

                uint textureId;
                GlNative.glGenTextures(1, &textureId);
                GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, textureId);
                // プラグインが残した GL_PIXEL_UNPACK_BUFFER 等の転送状態でアップロードが壊れないようにする
                GlContextManager.Shared?.ResetUnpackState();
                GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, unchecked((int)GlNative.GL_RGBA32F), width, height, 0, GlNative.GL_RGBA, GlNative.GL_FLOAT, buffer);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, (int)GlNative.GL_NEAREST);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, (int)GlNative.GL_NEAREST);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_S, (int)GlNative.GL_CLAMP_TO_EDGE);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_T, (int)GlNative.GL_CLAMP_TO_EDGE);
                GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, 0);

                return new OfxGlTexture(textureId, width, height, name);
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }

        /// <summary>
        /// 空 (未初期化) の GL_RGBA32F テクスチャを作成します。GL スレッド上で呼び出してください
        /// </summary>
        /// <param name="width">テクスチャの幅</param>
        /// <param name="height">テクスチャの高さ</param>
        /// <param name="hostOwned">ホストが所有するテクスチャかどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成されたテクスチャ</returns>
        public static OfxGlTexture CreateEmpty(int width, int height, bool hostOwned, string name)
        {
            uint textureId;
            GlNative.glGenTextures(1, &textureId);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, textureId);
            // GL_PIXEL_UNPACK_BUFFER がバインドされたままだと null がバッファ内オフセットとして解釈されてしまう
            GlContextManager.Shared?.ResetUnpackState();
            GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, unchecked((int)GlNative.GL_RGBA32F), width, height, 0, GlNative.GL_RGBA, GlNative.GL_FLOAT, null);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, (int)GlNative.GL_NEAREST);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, (int)GlNative.GL_NEAREST);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_S, (int)GlNative.GL_CLAMP_TO_EDGE);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_T, (int)GlNative.GL_CLAMP_TO_EDGE);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, 0);

            return new OfxGlTexture(textureId, width, height, name) { HostOwned = hostOwned };
        }

        /// <summary>
        /// テクスチャハンドル (プロパティセットのハンドル) から OfxGlTexture を取得します
        /// </summary>
        /// <param name="handle">テクスチャハンドル</param>
        /// <returns>対応する OfxGlTexture。存在しない場合は null</returns>
        public static OfxGlTexture? Resolve(nint handle)
        {
            return Registry.TryGetValue(handle, out var texture) ? texture : null;
        }

        /// <summary>
        /// テクスチャを破棄します。GL スレッド上で呼び出してください
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Registry.TryRemove(Handle, out _);
                var textureId = TextureId;
                GlNative.glDeleteTextures(1, &textureId);
                Properties.Dispose();
            }
        }
    }
}
