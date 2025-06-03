using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.IO
{
    abstract class RandomAccessReaderBase
    {
        public string FilePath { get; }

        public long Position { get; set; }

        public long Length { get; }

        protected SafeFileHandle FileHandle { get; }

        protected bool NeedBigEndianConvert { get; }

        internal protected RandomAccessReaderBase(string filePath, long position, SafeFileHandle fileHandle, bool needBigEndianConvert)
        {
            FilePath = filePath;
            Position = position;
            Length = RandomAccess.GetLength(fileHandle);
            FileHandle = fileHandle;
            NeedBigEndianConvert = needBigEndianConvert;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(Span<byte> buffer)
        {
            ValidateFileHandle();

            var readCount = RandomAccess.Read(FileHandle, buffer, Position);
            Position += readCount;
            return readCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            Span<byte> buffer = stackalloc byte[sizeof(byte)];

            if (Read(buffer) < buffer.Length)
            {
                throw new EndOfFileException();
            }

            return buffer[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(long length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            else if (length == 0)
            {
                return [];
            }

            ValidateFileHandle();

            length = Math.Min(length, Length - Position);
            var result = new byte[length];

            var read = RandomAccess.Read(FileHandle, result, Position);
            Position += read;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];

            return ReadUnmanaged<short>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];

            return ReadUnmanaged<ushort>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];

            return ReadUnmanaged<int>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];

            return ReadUnmanaged<uint>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];

            return ReadUnmanaged<long>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];

            return ReadUnmanaged<ulong>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];

            return ReadUnmanaged<float>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];

            return ReadUnmanaged<double>(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar()
        {
            Span<byte> buffer = stackalloc byte[sizeof(char)];

            return ReadUnmanaged<char>(buffer);
        }

        public char[] ReadChars(int length)
        {
            var result = new char[length];
            var buffer = MemoryMarshal.Cast<char, byte>(result);
            Read(buffer);

            for (var i = 0; i < buffer.Length; i += sizeof(char))
            {
                buffer.Slice(i, sizeof(char)).Reverse();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiString(int align = 1)
        {
            Span<byte> buffer = stackalloc byte[256];

            ValidateFileHandle();

            if (RandomAccess.Read(FileHandle, buffer[..1], Position) < 1)
            {
                throw new EndOfFileException();
            }
            Position++;
            var length = (int)buffer[0];
            if (length < 1)
            {
                Position += align - 1;
                return "";
            }

            var chars = new char[length];

            RandomAccess.Read(FileHandle, buffer[..length], Position);
            Position += length;

            if ((length + 1) % align != 0)
            {
                Position += (align - ((length + 1) % align));
            }

            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)buffer[i];
            }

            return new string(chars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T ReadUnmanaged<T>(Span<byte> buffer) where T : unmanaged
        {
            ValidateFileHandle();

            if (RandomAccess.Read(FileHandle, buffer, Position) < buffer.Length)
            {
                throw new EndOfFileException();
            }

            Position += buffer.Length;

            if (NeedBigEndianConvert)
            {
                buffer.Reverse();
            }

            return Unsafe.ReadUnaligned<T>(ref buffer[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateFileHandle()
        {
            if (FileHandle.IsClosed)
            {
                throw new InvalidOperationException("file is closed");
            }
            else if (FileHandle.IsInvalid)
            {
                throw new InvalidOperationException("file handle is invalid");
            }
        }
    }

    class RandomAccessFileReader : RandomAccessReaderBase, IDisposable
    {
        public RandomAccessFileReader(string filePath, bool isBigEndian, bool hitRandomAccess = false) : base(filePath, 0, File.OpenHandle(filePath, options: hitRandomAccess ? FileOptions.RandomAccess : FileOptions.None), isBigEndian && BitConverter.IsLittleEndian) { }

        public RandomAccessReaderBase CreateSubReader(long position)
        {
            return new RandomAccessFileSubReader(FilePath, position, FileHandle, NeedBigEndianConvert);
        }

        public void Dispose()
        {
            FileHandle.Dispose();
        }
    }

    file class RandomAccessFileSubReader : RandomAccessReaderBase
    {
        public RandomAccessFileSubReader(string filePath, long position, SafeFileHandle fileHandle, bool needBigEndianConvert) : base(filePath, position, fileHandle, needBigEndianConvert)
        {
        }
    }

    class EndOfFileException : Exception
    {
        public EndOfFileException() : base("end of file") { }
    }
}
