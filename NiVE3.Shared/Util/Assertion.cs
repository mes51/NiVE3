using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Util
{
    public class Assertion
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull<T>([NotNull] T? value, string? message = null)
        {
            if (value != null)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}
