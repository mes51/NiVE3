using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime.Win32;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Internal.Native
{
    static class PropVariant
    {
        [DllImport("Propsys.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern Result PropVariantToUInt32([In] ref Variant propvarIn, out uint pulRet);
    }
}
