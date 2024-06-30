using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class ShadowMap : IDisposable
    {
        public readonly Matrix4x4 LightViewProjectionMatrix;

        public readonly int[] Indices;

        public readonly int[] BufferIndices;

        public readonly int ShadowMapSize;

        public ShadowBuffer ShadowBuffer { get; }

        public ShadowMap(ShadowBuffer shadowBuffer, int shadowMapSize, Matrix4x4 lightViewProjectionMatrix)
        {
            ShadowMapSize = shadowMapSize;
            LightViewProjectionMatrix = lightViewProjectionMatrix;
            ShadowBuffer = shadowBuffer;
            Indices = ArrayPool<int>.Shared.Rent(shadowMapSize * shadowMapSize);
            Indices.AsSpan(0, shadowMapSize * shadowMapSize).Fill(-1);
            BufferIndices = ArrayPool<int>.Shared.Rent(shadowMapSize * shadowMapSize);
            BufferIndices.AsSpan(0, shadowMapSize * shadowMapSize).Fill(-1);

            shadowBuffer.AllocBuffer(shadowMapSize * shadowMapSize * 2);
        }

        public void AllocBuffer()
        {
            ShadowBuffer.AllocBuffer(ShadowMapSize * ShadowMapSize);
        }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(Indices);
            ArrayPool<int>.Shared.Return(BufferIndices);
        }
    }

    class ShadowBuffer : IDisposable
    {
        const int BufferSize = 1024 * 1024; // 32MB

        public readonly List<ShadowPixel[]> Buffers = [];

        int CurrentHeadIndex = -1;

        public void AllocBuffer(int length)
        {
            var remain = (Buffers.Count * BufferSize) - CurrentHeadIndex;
            var need = (int)Math.Ceiling(Math.Max(length - remain, 0) / (float)BufferSize);
            for (var i = need; i > 0; i--)
            {
                var newBuffer = ArrayPool<ShadowPixel>.Shared.Rent(BufferSize);
                newBuffer.AsSpan().Fill(ShadowPixel.Empty);
                Buffers.Add(newBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int bankIndex, int index) GetEmptyIndex()
        {
            var index = Interlocked.Increment(ref CurrentHeadIndex);
            return (index / BufferSize, index % BufferSize);
        }

        public void Dispose()
        {
            foreach (var b in Buffers)
            {
                ArrayPool<ShadowPixel>.Shared.Return(b);
            }
        }
    }

    record struct ShadowPixel(Vector4 Color, float Depth, int TriangleId, int NextIndex = -1, int NextBuffer = -1)
    {
        public static readonly ShadowPixel Empty = new ShadowPixel(Vector4.Zero, float.NegativeInfinity, -1);

        public readonly Vector4 Color = Color;

        public readonly float Depth = Depth;

        public readonly int TriangleId = TriangleId;

        public int NextIndex = NextIndex;

        public int NextBuffer = NextBuffer;
    }
}
