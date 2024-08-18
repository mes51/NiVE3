using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Effect
{
    static class NoiseFunction
    {
        const uint Multiplyer = 1664525U;

        const uint Increment = 1013904223U;

        // from https://jcgt.org/published/0009/03/02/paper.pdf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> Pcg3DUIntCpu(uint x, uint y, uint z, uint seed)
        {
            var vx = (x + seed) * Multiplyer + Increment;
            var vy = (y + seed) * Multiplyer + Increment;
            var vz = (z + seed) * Multiplyer + Increment;

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16;
            vy ^= vy >> 16;
            vz ^= vz >> 16;
            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;

            return Vector128.Create(vx, vy, vz, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Pcg3DFloatCpu(uint x, uint y, uint z, uint seed)
        {
            var result = Pcg3DUIntCpu(x, y, z, seed);
            return (Vector128.ConvertToSingle(result) / Vector128.Create((float)uint.MaxValue)).AsVector4();
        }

        // from https://jcgt.org/published/0009/03/02/paper.pdf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pcg3D1FloatCpu(uint x, uint y, uint z, uint seed)
        {
            var vx = (x + seed) * Multiplyer + Increment;
            var vy = (y + seed) * Multiplyer + Increment;
            var vz = (z + seed) * Multiplyer + Increment;

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16;
            vy ^= vy >> 16;
            vz ^= vz >> 16;
            
            return (vx + vy * vz) / (float)uint.MaxValue;
        }

        /// <summary>
        /// ((x, y, z), (x + 1, y, z), (x, y + 1, z), (x + 1, y + 1, z))
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        // from https://jcgt.org/published/0009/03/02/paper.pdf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> Pcg3D1Vector128UIntCpu(uint x, uint y, uint z, uint seed)
        {
            var vx = (Vector128.Create(x + seed) + Vector128.Create(0U, 1U, 0U, 1U)) * Multiplyer + Vector128.Create(Increment);
            var vy = (Vector128.Create(y + seed) + Vector128.Create(0U, 0U, 1U, 1U)) * Multiplyer + Vector128.Create(Increment);
            var vz = Vector128.Create(z + seed) * Multiplyer + Vector128.Create(Increment);

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16;
            vy ^= vy >> 16;
            vz ^= vz >> 16;

            return vx + vy * vz;
        }

        /// <summary>
        /// ((x, y, z), (x + 1, y, z), (x, y + 1, z), (x + 1, y + 1, z))
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="seed"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Pcg3D1Vector4Cpu(uint x, uint y, uint z, uint seed)
        {
            return (Vector128.ConvertToSingle(Pcg3D1Vector128UIntCpu(x, y, z, seed)) / uint.MaxValue).AsVector4();
        }

        // from https://jcgt.org/published/0009/03/02/paper.pdf
        public static UInt3 Pcg3DUIntGpu(UInt3 pos, uint seed)
        {
            var v = (pos + seed) * Multiplyer + Increment;
            v.X += v.Y * v.Z;
            v.Y += v.Z * v.X;
            v.Z += v.X * v.Y;
            v ^= v >> 16U;
            v.X += v.Y * v.Z;
            v.Y += v.Z * v.X;
            v.Z += v.X * v.Y;

            return v;
        }

        public static Float3 Pcg3DFloatGpu(UInt3 pos, uint seed)
        {
            var v = Pcg3DUIntGpu(pos, seed);
            return new Float3(v.X / (float)uint.MaxValue, v.Y / (float)uint.MaxValue, v.Z / (float)uint.MaxValue);
        }

        // from https://jcgt.org/published/0009/03/02/paper.pdf
        public static float Pcg3D1FloatGpu(UInt3 pos, uint seed)
        {
            var v = (pos + seed) * Multiplyer + Increment;
            v.X += v.Y * v.Z;
            v.Y += v.Z * v.X;
            v.Z += v.X * v.Y;
            v ^= v >> 16U;

            return (v.X + v.Y * v.Z) / (float)uint.MaxValue;
        }

        /// <summary>
        /// ((x, y, z), (x + 1, y, z), (x, y + 1, z), (x + 1, y + 1, z))
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        // from https://jcgt.org/published/0009/03/02/paper.pdf
        public static UInt4 Pcg3D1UInt4Gpu(UInt3 pos, uint seed)
        {
            var vx = ((pos.XXXX + seed) + new UInt4(0U, 1U, 0U, 1U)) * Multiplyer + Increment;
            var vy = ((pos.YYYY + seed) + new UInt4(0U, 0U, 1U, 1U)) * Multiplyer + Increment;
            var vz = (pos.ZZZZ + seed) * Multiplyer + Increment;

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16U;
            vy ^= vy >> 16U;
            vz ^= vz >> 16U;

            return vx + vy * vz;
        }

        /// <summary>
        /// ((x, y, z), (x + 1, y, z), (x, y + 1, z), (x + 1, y + 1, z))
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="seed"></param>
        public static Float4 Pcg3D1Float4Gpu(UInt3 pos, uint seed)
        {
            var v = Pcg3D1UInt4Gpu(pos, seed);
            return new Float4(v.X / (float)uint.MaxValue, v.Y / (float)uint.MaxValue, v.Z / (float)uint.MaxValue, v.W / (float)uint.MaxValue);
        }
    }
}
