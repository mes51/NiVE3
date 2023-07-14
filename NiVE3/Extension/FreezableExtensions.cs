using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Extension
{
    static class FreezableExtensions
    {
        public static T FreezeCurrentObject<T>(this T freezable) where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }
    }
}
