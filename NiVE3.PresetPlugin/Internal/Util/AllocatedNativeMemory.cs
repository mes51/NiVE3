using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Util
{
    readonly struct AllocatedNativeMemory : IComparable, IComparable<AllocatedNativeMemory>, IEquatable<AllocatedNativeMemory>, IDisposable
    {
        public static readonly AllocatedNativeMemory Null = new AllocatedNativeMemory();

        public readonly nint Ptr;

        public readonly int Size;

        public bool IsNull => Ptr == 0;

        public AllocatedNativeMemory(int size)
        {
            Ptr = Marshal.AllocHGlobal(size);
            Size = size;
        }

        public byte[] ToArray()
        {
            if (IsNull)
            {
                return [];
            }

            var result = new byte[Size];
            Marshal.Copy(Ptr, result, 0, Size);
            return result;
        }

        public void CopyFrom(byte[] data)
        {
            if (!IsNull)
            {
                Marshal.Copy(data, 0, Ptr, Math.Min(data.Length, Size));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object? obj)
        {
            if (obj is AllocatedNativeMemory other)
            {
                return CompareTo(other);
            }
            else if (obj is null)
            {
                return 1;
            }

            throw new ArgumentException(null, nameof(obj));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(AllocatedNativeMemory other)
        {
            return Ptr.CompareTo(other.Ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AllocatedNativeMemory other)
        {
            return Ptr == other.Ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is AllocatedNativeMemory other)
            {
                return Ptr == other.Ptr;
            }
            else if (obj is IntPtr ptr)
            {
                return Ptr == ptr;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Ptr.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Ptr.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ToStruct<T>() where T : unmanaged
        {
            return Marshal.PtrToStructure<T>(this);
        }

        public void Dispose()
        {
            if (Ptr != 0)
            {
                Marshal.FreeHGlobal(Ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator IntPtr(AllocatedNativeMemory obj)
        {
            return obj.Ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.Ptr == right.Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.Ptr != right.Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.CompareTo(right) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.CompareTo(right) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.CompareTo(right) <= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(AllocatedNativeMemory left, AllocatedNativeMemory right) => left.CompareTo(right) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AllocatedNativeMemory FromStruct<T>(in T value, int? size = null) where T : unmanaged
        {
            if (!size.HasValue)
            {
                size = Marshal.SizeOf<T>();
            }

            var result = new AllocatedNativeMemory(size.Value);
            Marshal.StructureToPtr<T>(value, result, false);
            return result;
        }
    }
}
