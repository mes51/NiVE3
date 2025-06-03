using NiVE3.PresetPlugin.Internal.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Extensions
{
    static class RandomAccessFileReaderExtension
    {
        static Dictionary<Type, ReverseFieldInfo[]> StructFields { get; } = [];

        public static T ReadStruct<T>(this RandomAccessFileReader reader) where T : struct
        {
            var data = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            if (BitConverter.IsLittleEndian)
            {
                var type = typeof(T);
                if (!StructFields.TryGetValue(type, out ReverseFieldInfo[]? fields))
                {
                    fields = GetReverseFieldInfo(type);
                    StructFields.Add(type, fields);
                }

                foreach (var field in fields)
                {
                    if (field.Size < 2)
                    {
                        continue;
                    }

                    var offset = field.Offset;
                    for (var i = 0; i < field.Count; i++)
                    {
                        Array.Reverse(data, offset, field.Size);
                        offset += field.Size;
                    }
                }
            }

            var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            }
            finally
            {
                ptr.Free();
            }
        }

        public static string ReadFixedSizeAsciiString(this RandomAccessFileReader reader, int length, bool trimEmpty = false)
        {
            var chars = new char[length];
            var data = reader.ReadBytes(length);

            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)data[i];
            }
            var result = new string(chars);

            if (trimEmpty)
            {
                return result.Trim().Trim('\0');
            }
            else
            {
                return result;
            }
        }

        public static string ReadUnicodeString(this RandomAccessFileReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 1)
            {
                return "";
            }

            var result = new string(reader.ReadChars(length));
            return result.Trim('\0');
        }

        static ReverseFieldInfo[] GetReverseFieldInfo(Type type, int offset = 0)
        {
            var charSet = type.StructLayoutAttribute?.CharSet ?? CharSet.Unicode;

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfo = new List<ReverseFieldInfo>();
            foreach (var field in fields)
            {
                var marshalAs = field.GetCustomAttribute<MarshalAsAttribute>();
                var size = DetermineSize(field.FieldType, marshalAs, charSet);

                if (size < 2)
                {
                    continue;
                }

                if ((field.FieldType.Namespace?.StartsWith("System") ?? false) || field.GetCustomAttribute<SingleBigEndianDataAttribute>() != null)
                {
                    var count = (marshalAs?.Value == UnmanagedType.ByValArray || marshalAs?.Value == UnmanagedType.ByValTStr)? marshalAs.SizeConst : 1;
                    fieldInfo.Add(new ReverseFieldInfo(Marshal.OffsetOf(type, field.Name).ToInt32() + offset, size, count));
                }
                else
                {
                    fieldInfo.AddRange(GetReverseFieldInfo(field.FieldType, Marshal.OffsetOf(type, field.Name).ToInt32()));
                }
            }

            return fieldInfo.ToArray();
        }

        static int DetermineSize(Type type, MarshalAsAttribute? marshalAs, CharSet charSet)
        {
            if (marshalAs != null)
            {
                return marshalAs.Value switch
                {
                    UnmanagedType.Bool => 4,
                    UnmanagedType.Error => 4,
                    UnmanagedType.FunctionPtr => IntPtr.Size,
                    UnmanagedType.I1 => 1,
                    UnmanagedType.I2 => 2,
                    UnmanagedType.I4 => 4,
                    UnmanagedType.I8 => 8,
                    UnmanagedType.R4 => 4,
                    UnmanagedType.R8 => 8,
                    UnmanagedType.SysInt => IntPtr.Size,
                    UnmanagedType.SysUInt => UIntPtr.Size,
                    UnmanagedType.U1 => 1,
                    UnmanagedType.U2 => 2,
                    UnmanagedType.U4 => 4,
                    UnmanagedType.U8 => 8,
                    UnmanagedType.ByValArray when type.IsArray => Marshal.SizeOf(type.GetElementType() ?? typeof(object)),
                    UnmanagedType.ByValTStr when type == typeof(string) => charSet == CharSet.Ansi ? 1 : 2,
                    _ => Marshal.SizeOf(type)
                };
            }
            else
            {
                return Marshal.SizeOf(type);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    class SingleBigEndianDataAttribute : Attribute { }

    record ReverseFieldInfo(int Offset, int Size, int Count);
}
