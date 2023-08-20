using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class IOExtensions
    {
        public static void WriteStruct<T>(this BinaryWriter writer, T value) where T : struct
        {
            var span = MemoryMarshal.CreateSpan(ref value, 1);
            writer.Write(MemoryMarshal.Cast<T, byte>(span));
        }
    }
}
