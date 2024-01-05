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
        const int BufferSize = 1024 * 1024 * 8; // 512MB

        public readonly Matrix4x4 LightViewProjectionMatrix;

        public readonly List<ShadowPixel[]> Buffers = new List<ShadowPixel[]>();

        public readonly int[] Indices;

        public readonly int[] BankIndices;

        public int ShadowMapSize;

        int CurrentHeadIndex = -1;

        public ShadowMap(int size, Matrix4x4 lightViewProjectionMatrix)
        {
            ShadowMapSize = size;
            LightViewProjectionMatrix = lightViewProjectionMatrix;
            Indices = ArrayPool<int>.Shared.Rent(size * size);
            Indices.AsSpan(0, size * size).Fill(-1);
            BankIndices = ArrayPool<int>.Shared.Rent(size * size);
            BankIndices.AsSpan(0, size * size).Fill(-1);

            AllocBuffer(size * size * 2);
        }

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
            ArrayPool<int>.Shared.Return(Indices);
            ArrayPool<int>.Shared.Return(BankIndices);
        }
    }

    record struct ShadowPixel(Vector4 Color, float Depth, int TriangleId, int NextIndex = -1, int NextBank = -1)
    {
        public static readonly ShadowPixel Empty = new ShadowPixel(Vector4.Zero, float.NegativeInfinity, -1);

        public readonly Vector4 Color = Color;

        public readonly float Depth = Depth;

        public readonly int TriangleId = TriangleId;

        public int NextIndex = NextIndex;

        public int NextBank = NextBank;
    }
}
